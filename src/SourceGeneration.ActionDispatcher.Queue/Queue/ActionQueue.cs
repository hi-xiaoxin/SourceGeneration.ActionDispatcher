using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SourceGeneration.ActionDispatcher.Internal;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace SourceGeneration.ActionDispatcher.Queue;

public class ActionQueueOptions<TAction>
{
    public string? QueueName { get; set; }
    public bool IsPersisted { get; set; } = true;
    public int MaxConcurrency { get; set; }
    public Func<TAction, object> IdSelector { get; set; } = null!;
}

internal class ActionQueue<TAction> : IHostedService where TAction : notnull
{
    private readonly ActionSubscriber _notifier;
    private readonly Channel<TAction> _channel;
    private readonly ConcurrentDictionary<object, BackgroundTask<TAction>> _runningsById = new();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IActionPersistenceService<TAction> _persistenceService;
    private readonly ILogger<ActionQueue<TAction>> _logger;
    private readonly int _maxConcurrency;
    private readonly bool _persisted;
    private readonly string? _queue;
    private readonly Func<TAction, object> _idSelector;

    private CancellationTokenSource? _stoppingCts;
    private Task[]? _workers;

    public ActionQueue(
        ActionQueueOptions<TAction> options,
        ActionSubscriber notifier,
        IActionPersistenceService<TAction> store,
        IServiceScopeFactory scopeFactory,
        ILogger<ActionQueue<TAction>> logger)
    {
        _notifier = notifier;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _persistenceService = store;
        _maxConcurrency = Math.Max(1, options.MaxConcurrency);
        _persisted = options.IsPersisted;
        _queue = options.QueueName;
        _idSelector = options.IdSelector ?? (static x => x);

        _channel = Channel.CreateUnbounded<TAction>(new UnboundedChannelOptions
        {
            SingleReader = _maxConcurrency == 1,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });
    }

    public bool Cancel(object taskId)
    {
        ArgumentNullException.ThrowIfNull(taskId);

        if (_runningsById.TryRemove(taskId, out var task))
        {
            task.Cancel();
            _notifier.Notify(DispatchStatus.Canceled, task.Data);
            return true;
        }
        return false;
    }

    public async ValueTask EnqueueAsync(IReadOnlyList<PersistedTask<TAction>> tasks)
    {
        if (tasks == null || tasks.Count == 0) return;

        if (_persisted)
        {
            await _persistenceService.SaveTaskAsync(tasks).ConfigureAwait(false);
        }

        await EnqueueCoreAsync(tasks).ConfigureAwait(false);
    }

    internal async Task EnqueueCoreAsync(IReadOnlyList<PersistedTask<TAction>> tasks)
    {
        foreach (var task in tasks)
        {
            var action = task.Action;
            var id = _idSelector(action);
            var backgroundTask = new BackgroundTask<TAction>(task);

            if (_runningsById.TryAdd(id, backgroundTask))
            {
                backgroundTask.SetStatus(DispatchStatus.WaitingToRun);
                _notifier.Notify(DispatchStatus.WaitingToRun, action);

                if (!_channel.Writer.TryWrite(action))
                    await _channel.Writer.WriteAsync(action).ConfigureAwait(false);
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _workers = [.. Enumerable.Range(0, _maxConcurrency).Select(_ => WorkerLoopAsync(_stoppingCts.Token))];
        
        if (_persisted)
        {
            var tasks = await _persistenceService
                .GetExecutableTasksAsync(_queue, cancellationToken)
                .ConfigureAwait(false);

            if (tasks != null && tasks.Count > 0)
            {
                await EnqueueCoreAsync(tasks).ConfigureAwait(false);
            }
        }
    }

    private static readonly TimeSpan _shutdownTimeout = TimeSpan.FromSeconds(5);

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.Complete();

        if (_stoppingCts is null || _stoppingCts.IsCancellationRequested || _workers == null)
            return;

        try
        {
            _stoppingCts.Cancel();

            await Task.WhenAll(_workers)
                .WaitAsync(_shutdownTimeout, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch { }
        finally
        {
            _stoppingCts.Dispose();
            _stoppingCts = null;
            _workers = null;
        }
    }

    private async Task WorkerLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var action in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                var id = _idSelector(action);

                if (!_runningsById.TryRemove(id, out var task) || task!.Status != DispatchStatus.WaitingToRun)
                {
                    // 若任务已被移除或标记为取消，则直接尝试清理持久化并跳过执行
                    if (_persisted)
                    {
                        try
                        {
                            await _persistenceService.DeleteTaskAsync(id, CancellationToken.None).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            if (_logger.IsEnabled(LogLevel.Warning))
                            {
                                _logger.LogWarning(ex, "跳过已取消任务时删除持久化失败，ID={id}", id);
                            }
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
                        _logger.LogError(ex, "执行任务时发生未处理异常，任务Id={TaskId}", action);
                }
                finally
                {
                    if (_persisted)
                    {
                        try
                        {
                            await _persistenceService.DeleteTaskAsync(id, CancellationToken.None).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            if (_logger.IsEnabled(LogLevel.Warning))
                                _logger.LogWarning(ex, "清理任务持久化时发送异常，任务Id={TaskId}", action);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
    }
}
