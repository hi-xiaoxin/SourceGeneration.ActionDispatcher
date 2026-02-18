using Microsoft.Extensions.DependencyInjection;

namespace SourceGeneration.ActionDispatcher;

internal sealed class BackgroundTask<TKey, TData>(TKey id, TData data) where TKey : notnull where TData : notnull
{
    private CancellationTokenSource? _cts;
    private readonly Lock _lock = new();

    private bool _canceling = false;
    private int _status;

    public TKey Id { get; } = id;
    public TData Data { get; } = data;

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

            await scope.ServiceProvider
                .GetRequiredService<IActionDispatcher>()
                .ExecuteAsync(Data!, cancellationToken)
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
