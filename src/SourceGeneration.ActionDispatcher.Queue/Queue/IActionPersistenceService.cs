using SourceGeneration.ActionDispatcher.Queue;

namespace SourceGeneration.ActionDispatcher;

public interface IActionPersistenceService<TKey, TData> where TKey : notnull where TData :notnull
{
    Task SaveTaskAsync(IEnumerable<PersistedTask<TKey, TData>> tasks, CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(TKey taskId, CancellationToken cancellationToken = default);
    Task<List<PersistedTask<TKey, TData>>> GetExecutableTasksAsync(string? queueName, CancellationToken cancellationToken = default);
    Task<List<PersistedTask<TKey, TData>>> GetScheduledTasksAsync(string? queueName, CancellationToken cancellationToken = default);
}

internal class NopBackgroundTaskPersistenceService<TKey, TData> : IActionPersistenceService<TKey, TData>
    where TKey : notnull where TData : notnull
{
    private readonly static List<PersistedTask<TKey, TData>> EmptyTasks = [];

    public Task DeleteTaskAsync(TKey taskId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<List<PersistedTask<TKey, TData>>> GetExecutableTasksAsync(string? queueName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(EmptyTasks);
    }

    public Task<List<PersistedTask<TKey, TData>>> GetScheduledTasksAsync(string? queueName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(EmptyTasks);
    }

    public Task SaveTaskAsync(IEnumerable<PersistedTask<TKey, TData>> tasks, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
