using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

namespace SourceGeneration.ActionDispatcher;

internal class BackgroundTask<T> : BackgroundTask where T : notnull
{
    public T Data { get; init; } = default!;

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return Services.GetRequiredService<IActionDispatcher>().ExecuteAsync(Data, cancellationToken);
    }
}

public abstract class BackgroundTask
{
    private CancellationTokenSource? _cts;

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    private bool _canceling = false;
    private int _status;

    [JsonIgnore] public long Id { get; set; }
    [JsonIgnore] public bool Scheduled { get; internal set; }
    [JsonIgnore] public IServiceProvider Services { get; private set; } = null!;
    [JsonIgnore] public TaskStatus Status => (TaskStatus)Volatile.Read(ref _status);

    internal void SetStatus(TaskStatus status)
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


    internal async Task InternalExecuteAsync(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
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
            Services = scope.ServiceProvider;

            await ExecuteAsync(linked.Token).ConfigureAwait(false);
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

    protected abstract Task ExecuteAsync(CancellationToken cancellationToken);
}
