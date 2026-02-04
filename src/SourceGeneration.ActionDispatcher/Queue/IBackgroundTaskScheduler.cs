namespace SourceGeneration.ActionDispatcher;

public interface IBackgroundTaskScheduler<TTask> where TTask : BackgroundTask
{
    ValueTask ScheduleAsync(TTask task, long scheduledAtMs = 0) => ScheduleAsync([task], scheduledAtMs);
    ValueTask ScheduleAsync(IReadOnlyList<TTask> tasks, long scheduledAtMs = 0);

    bool Cancel(long taskId);
}
