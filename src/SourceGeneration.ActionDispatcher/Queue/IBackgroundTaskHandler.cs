namespace SourceGeneration.ActionDispatcher;

public interface IBackgroundTaskHandler<T>
{
    Task ExecuteAsync(IBackgroundTaskContext<T> context, CancellationToken cancellationToken);
}

public interface IBackgroundTaskContext<T>
{
    long Id { get; }
    T Data { get; }
}

internal class BackgroundTaskContext<T> : IBackgroundTaskContext<T>
{
    public long Id { get; init; }

    public long CreatedAt { get; init; }
    public T Data { get; init; } = default!;
}

