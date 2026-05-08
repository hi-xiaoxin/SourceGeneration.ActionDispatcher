namespace SourceGeneration.ActionDispatcher.Queue;

public interface IActionQueuePersistenceService
{
    Task SaveAsync<TAction>(IEnumerable<ActionTask<TAction>> tasks, CancellationToken cancellationToken = default) where TAction : notnull;
    Task<List<ActionTask<TAction>>> GetTasksAsync<TAction>(string? queueName, CancellationToken cancellationToken = default) where TAction : notnull;
    Task DeleteAsync(string? queueName, object id, CancellationToken cancellationToken = default);
}

internal sealed class ActionQueueNopPersistenceService : IActionQueuePersistenceService
{
    public Task DeleteAsync(string? queueName, object id, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<List<ActionTask<TAction>>> GetTasksAsync<TAction>(string? queueName, CancellationToken cancellationToken = default) where TAction : notnull
    {
        return Task.FromResult<List<ActionTask<TAction>>>([]);
    }

    public Task SaveAsync<TAction>(IEnumerable<ActionTask<TAction>> tasks, CancellationToken cancellationToken = default) where TAction : notnull
    {
        return Task.CompletedTask;
    }
}