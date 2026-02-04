namespace SourceGeneration.ActionDispatcher;

public interface IBackgroundTaskPersistenceService<TTask> where TTask : BackgroundTask
{
    Task SaveTaskAsync(string? queueName, IEnumerable<TTask> tasks, long scheduledTime, CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(long taskId, CancellationToken cancellationToken = default);
    Task<List<ScheduledTask<TTask>>> GetExecutableTasksAsync(string? queueName, CancellationToken cancellationToken = default);
    Task<List<ScheduledTask<TTask>>> GetScheduledTasksAsync(string? queueName, CancellationToken cancellationToken = default);
}

internal class NopBackgroundTaskPersistenceService<TTask> : IBackgroundTaskPersistenceService<TTask> where TTask : BackgroundTask
{
    public Task DeleteTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<List<ScheduledTask<TTask>>> GetScheduledTasksAsync(string? queueName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<List<ScheduledTask<TTask>>>([]);
    }

    public Task<List<ScheduledTask<TTask>>> GetExecutableTasksAsync(string? queueName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<List<ScheduledTask<TTask>>>([]);
    }

    public Task SaveTaskAsync(string? queueName, IEnumerable<TTask> tasks, long scheduledTime, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public class ScheduledTask<TTask> where TTask : BackgroundTask
{
    public TTask Task { get; set; } = null!;
    public long ScheduledAtMs { get; set; }
    public long CreatedAt { get; set; }
}