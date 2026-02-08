namespace SourceGeneration.ActionDispatcher;

public interface IBackgroundTaskPersistenceService<T>
{
    Task SaveTaskAsync(IEnumerable<PersistedTask<T>> tasks, CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<List<PersistedTask<T>>> GetExecutableTasksAsync(string? queueName, CancellationToken cancellationToken = default);
    Task<List<PersistedTask<T>>> GetScheduledTasksAsync(string? queueName, CancellationToken cancellationToken = default);
}

internal class NopBackgroundTaskPersistenceService<T> : IBackgroundTaskPersistenceService<T>
{
    public Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<List<PersistedTask<T>>> GetScheduledTasksAsync(string? queueName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<List<PersistedTask<T>>>([]);
    }

    public Task<List<PersistedTask<T>>> GetExecutableTasksAsync(string? queueName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<List<PersistedTask<T>>>([]);
    }

    public Task SaveTaskAsync(IEnumerable<PersistedTask<T>> tasks, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
