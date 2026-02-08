using Microsoft.Extensions.DependencyInjection;

namespace SourceGeneration.ActionDispatcher;

internal sealed class BackgroundTask<T>
{
    private CancellationTokenSource? _cts;

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    private bool _canceling = false;
    private int _status;

    public T Data { get; init; } = default!;

    public Guid Id { get; set; }
    public long BusinessId { get; set; }
    public TaskStatus Status => (TaskStatus)Volatile.Read(ref _status);

    public void SetStatus(TaskStatus status)
    {
        Interlocked.Exchange(ref _status, (int)status);
        if (status >= TaskStatus.RanToCompletion)
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
                SetStatus(TaskStatus.Canceled);
                return;
            }
            else
            {
                SetStatus(TaskStatus.Running);
                linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _cts = linked;
            }
        }

        try
        {
            using var scope = scopeFactory.CreateScope();

            await scope.ServiceProvider
                .GetRequiredService<IActionDispatcher>()
                .ExecuteAsync(Data!, cancellationToken)
                .ConfigureAwait(false);

            SetStatus(TaskStatus.RanToCompletion);
        }
        catch (OperationCanceledException)
        {
            SetStatus(TaskStatus.Canceled);
        }
        catch
        {
            SetStatus(TaskStatus.Faulted);
        }
    }
}
