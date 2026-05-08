namespace SourceGeneration.ActionDispatcher.Queue;

public interface IActionQueuePersistenceService
{
    Task SaveAsync<TAction>(IEnumerable<ActionQueueTask<TAction>> tasks, CancellationToken cancellationToken = default) where TAction : notnull;
    Task<List<ActionQueueTask<TAction>>> GetTasksAsync<TAction>(string? queueName, CancellationToken cancellationToken = default) where TAction : notnull;
    Task DeleteAsync(string? queueName, object key, CancellationToken cancellationToken = default);
}

internal sealed class ActionQueueNopPersistenceService : IActionQueuePersistenceService
{
    public Task DeleteAsync(string? queueName, object key, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<List<ActionQueueTask<TAction>>> GetTasksAsync<TAction>(string? queueName, CancellationToken cancellationToken = default) where TAction : notnull
    {
        return Task.FromResult<List<ActionQueueTask<TAction>>>([]);
    }

    public Task SaveAsync<TAction>(IEnumerable<ActionQueueTask<TAction>> tasks, CancellationToken cancellationToken = default) where TAction : notnull
    {
        return Task.CompletedTask;
    }
}