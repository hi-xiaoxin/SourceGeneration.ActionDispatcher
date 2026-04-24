namespace SourceGeneration.ActionDispatcher.Queue;

public interface IActionScheduledQueue<TAction> where TAction : notnull
{
    ValueTask ScheduleAsync(IReadOnlyList<TAction> items, long scheduledMs = 0);
    bool Cancel(object taskId);
}
