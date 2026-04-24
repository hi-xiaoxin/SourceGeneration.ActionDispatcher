using SourceGeneration.ActionDispatcher.Internal;

namespace SourceGeneration.ActionDispatcher.Queue;

internal class NopActionScheduledQueue<TAction>(ActionExecutor executor) : IActionScheduledQueue<TAction>
    where TAction : notnull
{
    public bool Cancel(object taskId) => false;

    public ValueTask ScheduleAsync(IReadOnlyList<TAction> items, long scheduledMs = 0)
    {
        foreach(var item in items)
        {
            executor.Execute(item);
        }
        return ValueTask.CompletedTask;
    }
}