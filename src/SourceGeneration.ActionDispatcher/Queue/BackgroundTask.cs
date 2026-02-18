using Microsoft.Extensions.DependencyInjection;

namespace SourceGeneration.ActionDispatcher;

internal sealed class BackgroundTask<TKey, TData> where TKey : notnull where TData : notnull
{
    private CancellationTokenSource? _cts;

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    private bool _canceling = false;
    private int _status;

    public TKey Id { get; set; } = default!;
    public TData Data { get; init; } = default!;

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
