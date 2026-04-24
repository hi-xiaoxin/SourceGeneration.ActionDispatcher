using SourceGeneration.ActionDispatcher.Internal;

namespace SourceGeneration.ActionDispatcher.Queue;

internal class NopActionScheduledQueue<TKey, TData>(ActionExecutor executor) : IActionScheduledQueue<TKey, TData>
    where TKey : notnull
    where TData : notnull
{
    public bool Cancel(TKey taskId) => false;

    public ValueTask ScheduleAsync(IReadOnlyList<DispatchItem<TKey, TData>> items, long scheduledAtMs = 0)
    {
        foreach(var item in items)
        {
            executor.Execute(item.Data);
        }
        return ValueTask.CompletedTask;
    }
}