using SourceGeneration.ActionDispatcher.Queue;

namespace SourceGeneration.ActionDispatcher;

public interface IActionPersistenceService<TAction> where TAction :notnull
{
    Task SaveTaskAsync(IEnumerable<PersistedTask<TAction>> tasks, CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(object id, CancellationToken cancellationToken = default);
    Task<List<PersistedTask<TAction>>> GetExecutableTasksAsync(string? queueName, CancellationToken cancellationToken = default);
    Task<List<PersistedTask<TAction>>> GetScheduledTasksAsync(string? queueName, CancellationToken cancellationToken = default);
}

internal class NopBackgroundTaskPersistenceService<TAction> : IActionPersistenceService<TAction> where TAction : notnull
{
    private readonly static List<PersistedTask<TAction>> EmptyTasks = [];

    public Task DeleteTaskAsync(object id, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<List<PersistedTask<TAction>>> GetExecutableTasksAsync(string? queueName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(EmptyTasks);
    }

    public Task<List<PersistedTask<TAction>>> GetScheduledTasksAsync(string? queueName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(EmptyTasks);
    }

    public Task SaveTaskAsync(IEnumerable<PersistedTask<TAction>> tasks, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
