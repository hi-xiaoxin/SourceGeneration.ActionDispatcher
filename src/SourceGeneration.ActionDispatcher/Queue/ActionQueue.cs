using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SourceGeneration.ActionDispatcher.Internal;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace SourceGeneration.ActionDispatcher.Queue;

public class ActionQueueOptions
{
    public string? QueueName { get; set; }
    public bool IsPersisted { get; set; } = true;
    public int MaxConcurrency { get; set; }
}

internal class ActionQueue<TKey, TData> : IHostedService
    where TKey : notnull
    where TData : notnull
{
    private readonly ActionSubscriber _notifier;
    private readonly Channel<TKey> _channel;
    private readonly ConcurrentDictionary<TKey, BackgroundTask<TKey, TData>> _runningsById = new();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IActionPersistenceService<TKey, TData> _store;
    private readonly ILogger<ActionQueue<TKey, TData>> _logger;
    private readonly int _maxConcurrency;
    private readonly bool _persisted;
    private readonly string? _queue;

    private CancellationTokenSource? _stoppingCts;
    private Task[]? _workers;

    public ActionQueue(
        ActionQueueOptions options,
        ActionSubscriber notifier,
        IActionPersistenceService<TKey, TData> store,
        IServiceScopeFactory scopeFactory,
        ILogger<ActionQueue<TKey, TData>> logger)
    {
        _notifier = notifier;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _store = store;
        _maxConcurrency = Math.Max(1, options.MaxConcurrency);
        _persisted = options.IsPersisted;
        _queue = options.QueueName;
        _channel = Channel.CreateUnbounded<TKey>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    public bool Cancel(TKey taskId)
    {
        if (_runningsById.TryRemove(taskId, out var task))
        {
            task.Cancel();
            _notifier.Notify(DispatchStatus.Canceled, task.Data);
            return true;
        }
        return false;
    }

    public async ValueTask EnqueueAsync(IReadOnlyList<PersistedTask<TKey, TData>> tasks)
    {
        if (tasks == null || tasks.Count == 0) return;
        if (_persisted)
        {
            await _store.SaveTaskAsync(tasks).ConfigureAwait(false);
        }

        await EnqueueCoreAsync(tasks).ConfigureAwait(false);
    }

    internal async Task EnqueueCoreAsync(IReadOnlyList<PersistedTask<TKey, TData>> tasks)
    {
        foreach (var task in tasks)
        {
            var id = task.Id;
            BackgroundTask<TKey, TData> backgroundTask = new(id, task.Data);

            if (_runningsById.TryAdd(id, backgroundTask))
            {
                backgroundTask.SetStatus(DispatchStatus.WaitingToRun);
                _notifier.Notify(DispatchStatus.WaitingToRun, task.Data);

                if (!_channel.Writer.TryWrite(id))
                    await _channel.Writer.WriteAsync(id).ConfigureAwait(false);
            }
        }
    }


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _workers = [.. Enumerable.Range(0, _maxConcurrency).Select(_ => WorkerLoopAsync(_stoppingCts.Token))];
        if (_persisted)
        {
            var tasks = await _store.GetExecutableTasksAsync(_queue, cancellationToken).ConfigureAwait(false);

            if (tasks != null && tasks.Count > 0)
            {
                await EnqueueCoreAsync(tasks).ConfigureAwait(false);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.Complete();

        if (_stoppingCts != null)
        {
            try
            {
                _stoppingCts.Cancel();
            }
            finally
            {
                var timeout = TimeSpan.FromSeconds(5);
                if (_workers != null)
                {
                    try
                    {
                        await Task.WhenAll(_workers).WaitAsync(timeout, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch { }
                }

                _stoppingCts.Dispose();
                _stoppingCts = null;
            }
        }
    }

    private async Task WorkerLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var taskId in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!_runningsById.TryRemove(taskId, out var task) || task!.Status != DispatchStatus.WaitingToRun)
                {
                    // 若任务已被移除或标记为取消，则直接尝试清理持久化并跳过执行
                    if (_persisted)
                    {
                        try
                        {
                            await _store.DeleteTaskAsync(taskId, CancellationToken.None).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            if (_logger.IsEnabled(LogLevel.Warning))
                                _logger.LogWarning(ex, "跳过已取消任务时删除持久化失败，taskId={TaskId}", taskId);
                        }
                    }
                    continue;
                }

                // business mapping already removed in TryRemoveById

                try
                {
                    await task.InternalExecuteAsync(_scopeFactory, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    if (_logger.IsEnabled(LogLevel.Error))
                        _logger.LogError(ex, "执行任务时发生未处理异常，任务Id={TaskId}", taskId);
                }
                finally
                {
                    if (_persisted)
                    {
                        try
                        {
                            await _store.DeleteTaskAsync(taskId, CancellationToken.None).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            if (_logger.IsEnabled(LogLevel.Warning))
                                _logger.LogWarning(ex, "清理任务持久化时发送异常，任务Id={TaskId}", taskId);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
    }
}
