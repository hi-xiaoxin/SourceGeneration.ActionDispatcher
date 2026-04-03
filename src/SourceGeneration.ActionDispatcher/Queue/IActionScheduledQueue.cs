namespace SourceGeneration.ActionDispatcher.Queue;

public interface IActionScheduledQueue<TKey, TData>
    where TKey : notnull
    where TData : notnull
{
    ValueTask ScheduleAsync(IReadOnlyList<DispatchItem<TKey, TData>> items, long scheduledAtMs = 0);
    bool Cancel(TKey taskId);
}
