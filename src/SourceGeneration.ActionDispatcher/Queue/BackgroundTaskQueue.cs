using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace SourceGeneration.ActionDispatcher;

public class BackgroundTaskQueueOptions
{
    public string? QueueName { get; set; }
    public bool IsPersisted { get; set; } = true;
    public int MaxConcurrency { get; set; }
}

internal class BackgroundTaskQueue<TTask> : IHostedService where TTask : BackgroundTask
{
    private readonly Channel<long> _channel;
    private readonly ConcurrentDictionary<long, TTask> _runnings = [];
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBackgroundTaskPersistenceService<TTask> _store;
    private readonly ILogger<BackgroundTaskQueue<TTask>> _logger;
    private readonly int _maxConcurrency;
    private readonly bool _persisted;
    private readonly string? _queue;

    private CancellationTokenSource? _stoppingCts;
    private Task[]? _workers;

    public BackgroundTaskQueue(
        BackgroundTaskQueueOptions options,
        IBackgroundTaskPersistenceService<TTask> store,
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundTaskQueue<TTask>> logger)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _store = store;
        _maxConcurrency = Math.Max(1, options.MaxConcurrency);
        _persisted = options.IsPersisted;
        _queue = options.QueueName;
        _channel = Channel.CreateUnbounded<long>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    public bool Cancel(long taskId)
    {
        if (_runnings.TryRemove(taskId, out var task))
        {
            task.Cancel();
            return true;
        }
        return false;
    }

    public async ValueTask EnqueueAsync(IReadOnlyList<TTask> tasks)
    {
        if (tasks == null || tasks.Count == 0) return;

        if (_persisted)
        {
            await _store.SaveTaskAsync(_queue, tasks, 0).ConfigureAwait(false);
        }
        await EnqueueCoreAsync(tasks).ConfigureAwait(false);
    }

    internal async Task EnqueueCoreAsync(IReadOnlyList<TTask> tasks)
    {
        foreach (var task in tasks)
        {
            var taskId = task.Id;
            _runnings.TryAdd(taskId, task);

            task.SetStatus(TaskStatus.WaitingToRun);

            if (!_channel.Writer.TryWrite(taskId))
            {
                await _channel.Writer.WriteAsync(taskId).ConfigureAwait(false);
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
                await EnqueueCoreAsync([.. tasks.OrderBy(x => x.CreatedAt).Select(x => x.Task)]).ConfigureAwait(false);
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
                if (!_runnings.TryRemove(taskId, out var task) || task.Status != TaskStatus.WaitingToRun)
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
