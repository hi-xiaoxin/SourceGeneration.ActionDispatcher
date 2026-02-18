using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SourceGeneration.ActionDispatcher.Internal;
using System.Collections.Concurrent;

namespace SourceGeneration.ActionDispatcher;

internal class ActionScheduledQueue<TKey, TData>(
    ActionQueueOptions options,
    ActionSubscriber notifier,
    ActionQueue<TKey, TData> queue,
    IActionPersistenceService<TKey, TData> store,
    ILogger<ActionScheduledQueue<TKey, TData>> logger)
    : IHostedService
    where TData : notnull
    where TKey : notnull
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    private readonly ILogger<ActionScheduledQueue<TKey, TData>> _logger = logger;
    private readonly ActionSubscriber _notifier = notifier;
    private readonly PriorityQueue<long, long> _pq = new();
    private readonly Dictionary<long, List<PersistedTask<TKey, TData>>> _scheduledMap = [];
    private readonly ConcurrentDictionary<TKey, long> _scheduledIndexById = new();

    private readonly bool _persisted = options.IsPersisted;
    private readonly string? _queue = options.QueueName;

    private CancellationTokenSource? _wakeUp;
    private CancellationTokenSource? _stoppingCts;

    private Task? _schedulerLoopTask;
    private int _wakeSignaled;

    public async ValueTask ScheduleAsync(IReadOnlyList<DispatchItem<TKey, TData>> items, long scheduledAtMs = 0)
    {
        if (items == null || items.Count == 0) return;

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var tasks = items.Select(x => new PersistedTask<TKey, TData>
        {
            Id = x.Id,
            CreatedAt = now,
            Data = x.Data,
            Queue = _queue,
            ScheduledAtMs = scheduledAtMs,
        }).ToList();

        if (_persisted)
        {
            await store.SaveTaskAsync(tasks).ConfigureAwait(false);
        }

        if (scheduledAtMs == 0 || scheduledAtMs <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        {
            await queue.EnqueueCoreAsync(tasks).ConfigureAwait(false);
        }
        else
        {
            await ScheduleCoreAsync(tasks, scheduledAtMs).ConfigureAwait(false);
        }
    }

    private async ValueTask ScheduleCoreAsync(List<PersistedTask<TKey, TData>> tasks, long scheduledAtMs)
    {
        bool shouldWake = false;

        foreach (var task in tasks)
        {
            _notifier.Notify(DispatchStatus.WaitingForActivation, task.Data!);
        }

        lock (_lock)
        {
            var previousEarliest = _pq.Count > 0 ? _pq.Peek() : (long?)null;

            if (!_scheduledMap.TryGetValue(scheduledAtMs, out var list))
            {
                list = new List<PersistedTask<TKey, TData>>(tasks.Count);
                _scheduledMap[scheduledAtMs] = list;
                _pq.Enqueue(scheduledAtMs, scheduledAtMs);
            }
            list.AddRange(tasks);

            // 更新索引（在锁内添加映射，确保一致性）      
            foreach (var t in tasks)
            {
                _scheduledIndexById.TryAdd(t.Id, scheduledAtMs);
            }

            if (!previousEarliest.HasValue || scheduledAtMs < previousEarliest.Value)
            {
                shouldWake = true;
                Interlocked.Exchange(ref _wakeSignaled, 1);
            }
        }

        // 在锁外唤醒（避免在锁内触发 continuation）
        if (shouldWake)
        {
            var c = Volatile.Read(ref _wakeUp);
            try { c?.Cancel(); } catch { }
        }
    }

    public bool Cancel(TKey taskId)
    {
        if (_persisted)
        {
            // 从持久化删除（fire-and-forget）
            _ = Task.Run(async () =>
            {
                try
                {
                    await store.DeleteTaskAsync(taskId, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (_logger.IsEnabled(LogLevel.Warning))
                        _logger.LogWarning(ex, "取消计划任务时删除持久化失败，taskId={TaskId}", taskId);
                }
            });
        }

        // 先尝试从 scheduledIndex 快速定位并移除
        if (!_scheduledIndexById.TryRemove(taskId, out var scheduledAt))
        {
            // 可能已入队或在运行，转发给底层 queue
            return queue.Cancel(taskId);
        }

        PersistedTask<TKey, TData>? removed = null;
        lock (_lock)
        {
            if (_scheduledMap.TryGetValue(scheduledAt, out var list))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    removed = list[i];
                    if (removed.Id.Equals(taskId))
                    {
                        list.RemoveAt(i);
                        break;
                    }
                }

                if (list.Count == 0)
                {
                    _scheduledMap.Remove(scheduledAt);
                    // 标记唤醒以便 scheduler 重新计算下次触发（因为 earliest 可能变化）
                    if (Interlocked.Exchange(ref _wakeSignaled, 1) == 0)
                    {
                        // 唤醒当前安装的 CTS（若存在）
                        var c = Volatile.Read(ref _wakeUp);
                        try { c?.Cancel(); } catch { }
                    }

                }
            }
        }

        if (removed != null)
        {
            _notifier.Notify(DispatchStatus.Canceled, removed.Data);
            return true;
        }

        return false;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _schedulerLoopTask = SchedulerLoopAsync(_stoppingCts.Token);

        if (_persisted)
        {
            var tasks = await store.GetScheduledTasksAsync(_queue, cancellationToken).ConfigureAwait(false);
            if (tasks != null && tasks.Count > 0)
            {
                foreach (var group in tasks.GroupBy(x => x.ScheduledAtMs).OrderBy(x => x.Key))
                {
                    await ScheduleCoreAsync([.. group], group.Key).ConfigureAwait(false);
                }
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_stoppingCts != null)
        {
            try
            {
                _stoppingCts.Cancel();
                var tcs = Interlocked.Exchange(ref _wakeUp, null);

                var c = Interlocked.Exchange(ref _wakeUp, null);
                if (c != null)
                {
                    try { c.Cancel(); } catch { }
                    try { c.Dispose(); } catch { }
                }
            }
            finally
            {
                var timeout = TimeSpan.FromSeconds(5);
                if (_schedulerLoopTask != null)
                {
                    try
                    {
                        await _schedulerLoopTask.WaitAsync(timeout, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch { }
                }

                _stoppingCts.Dispose();
                _stoppingCts = null;
            }
        }
    }

    private async Task SchedulerLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            long? waitUntil = null;
            List<List<PersistedTask<TKey, TData>>>? listsToFire = null;

            lock (_lock)
            {
                // 先修剪 PQ 中已不存在于 scheduledMap 的条目，避免 Peek 返回无效时间
                while (_pq.Count > 0 && !_scheduledMap.ContainsKey(_pq.Peek()))
                    _pq.Dequeue();

                if (_pq.Count > 0)
                {
                    var time = _pq.Peek();

                    waitUntil = time;
                    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (time <= now)
                    {
                        List<long> timesToRemove = [];
                        while (_pq.Count > 0 && _pq.Peek() <= now)
                            timesToRemove.Add(_pq.Dequeue());

                        if (timesToRemove.Count > 0)
                        {
                            listsToFire = new List<List<PersistedTask<TKey, TData>>>(timesToRemove.Count);
                            foreach (var t in timesToRemove)
                            {
                                if (_scheduledMap.TryGetValue(t, out var list))
                                {
                                    _scheduledMap.Remove(t);
                                    if (list.Count > 0)
                                        listsToFire.Add(list);
                                }
                            }
                        }
                    }
                }
            }

            if (listsToFire != null && listsToFire.Count > 0)
            {
                // 在锁外合并并触发，减少临界区时间
                List<PersistedTask<TKey, TData>> toFire = [];
                foreach (var l in listsToFire)
                    toFire.AddRange(l);

                // 移除索引条目（已经从 scheduledMap 移除），避免内存增长
                foreach (var t in toFire)
                {
                    _scheduledIndexById.TryRemove(t.Id, out _);
                }

                try
                {
                    await queue.EnqueueCoreAsync(toFire).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "将计划任务入队时出错");
                }

                continue; // 立即循环检查下一个
            }

            // 安装本地 CTS（原子）
            var localWake = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var installed = Interlocked.CompareExchange(ref _wakeUp, localWake, null);
            var currentWake = installed ?? localWake;
            var waitToken = currentWake.Token;

            // 如果在安装前有唤醒信号，立即完成 waitOn
            if (Interlocked.Exchange(ref _wakeSignaled, 0) == 1)
            {
                try { currentWake.Cancel(); } catch { }
            }

            // 计算等待时间并等待，或被 _wakeUp 唤醒
            TimeSpan delay = waitUntil.HasValue
                ? TimeSpan.FromMilliseconds(Math.Max(0, waitUntil.Value - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))
                : TimeSpan.FromMinutes(15);

            try
            {
                await Task.Delay(delay, waitToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            finally
            {
                Interlocked.CompareExchange(ref _wakeUp, null, localWake);
                try { localWake.Dispose(); } catch { }
            }
        }
    }
}
