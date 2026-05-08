using SourceGeneration.ActionDispatcher.Queue;

namespace SourceGeneration.ActionDispatcher;

public interface IActionQueuePersistenceService
{
    Task SaveTaskAsync<TAction>(IEnumerable<PersistedActionTask<TAction>> tasks, CancellationToken cancellationToken = default) where TAction : notnull;
    Task<List<PersistedActionTask<TAction>>> GetExecutableTasksAsync<TAction>(string? queueName, CancellationToken cancellationToken = default) where TAction : notnull;
    Task<List<PersistedActionTask<TAction>>> GetScheduledTasksAsync<TAction>(string? queueName, CancellationToken cancellationToken = default) where TAction : notnull;
    Task DeleteTaskAsync(string? queueName, object id, CancellationToken cancellationToken = default);
}

internal sealed class ActionQueueNopPersistenceService : IActionQueuePersistenceService
{
    public Task DeleteTaskAsync(string? queueName, object id, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<List<PersistedActionTask<TAction>>> GetExecutableTasksAsync<TAction>(string? queueName, CancellationToken cancellationToken = default) where TAction : notnull
    {
        return Task.FromResult<List<PersistedActionTask<TAction>>>([]);
    }

    public Task<List<PersistedActionTask<TAction>>> GetScheduledTasksAsync<TAction>(string? queueName, CancellationToken cancellationToken = default) where TAction : notnull
    {
        return Task.FromResult<List<PersistedActionTask<TAction>>>([]);
    }

    public Task SaveTaskAsync<TAction>(IEnumerable<PersistedActionTask<TAction>> tasks, CancellationToken cancellationToken = default) where TAction : notnull
    {
        return Task.CompletedTask;
    }
}