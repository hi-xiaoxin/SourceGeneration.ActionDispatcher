using Microsoft.Extensions.DependencyInjection;

namespace SourceGeneration.ActionDispatcher.Queue;

internal sealed class BackgroundTask<TAction>(PersistedTask<TAction> task) where TAction : notnull
{
    private CancellationTokenSource? _cts;
    private readonly Lock _lock = new();

    private bool _canceling = false;
    private int _status;

    public TAction Data { get; } = task.Action;
    public long ScheduledAtMs { get; } = task.ScheduledMs;
    public long CreatedAt { get; } = task.CreatedMs;
    public string? Queue { get; } = task.Queue;

    public DispatchStatus Status => (DispatchStatus)Volatile.Read(ref _status);

    public void SetStatus(DispatchStatus status)
    {
        Interlocked.Exchange(ref _status, (int)status);
        if (status >= DispatchStatus.Succeeded)
        {
            _cts?.Dispose();
            _cts = null;
        }
    }

    public void Cancel()
    {
        lock (_lock)
        {
            _canceling = true;
            _cts?.Cancel();
        }
    }


    public async Task InternalExecuteAsync(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
    {
        CancellationTokenSource? linked;
        lock (_lock)
        {
            if (_canceling)
            {
                SetStatus(DispatchStatus.Canceled);
                return;
            }
            else
            {
                SetStatus(DispatchStatus.Running);
                linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _cts = linked;
            }
        }

        try
        {
            using var scope = scopeFactory.CreateScope();

            var context = new ActionTaskQueueContext<TAction>
            {
                Action = Data,
                ScheduledAtMs = ScheduledAtMs,
                CreatedAt = CreatedAt,
                Queue = task.Queue,
            };
            await scope.ServiceProvider
                .GetRequiredService<IActionDispatcher>()
                .ExecuteAsync(context, cancellationToken)
                .ConfigureAwait(false);

            SetStatus(DispatchStatus.Succeeded);
        }
        catch (OperationCanceledException)
        {
            SetStatus(DispatchStatus.Canceled);
        }
        catch
        {
            if (cancellationToken.IsCancellationRequested)
            {
                SetStatus(DispatchStatus.Canceled);
            }
            else
            {
                SetStatus(DispatchStatus.Faulted);
            }
        }
    }
}
