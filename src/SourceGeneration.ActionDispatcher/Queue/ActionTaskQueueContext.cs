namespace SourceGeneration.ActionDispatcher.Queue;

internal interface IActionTaskQueueContext
{
    object Data { get; }
}

public class ActionTaskQueueContext<TData> : ActionTaskQueueContext<Guid, TData> where TData : notnull
{

}
public class ActionTaskQueueContext<TKey, TData> : IActionTaskQueueContext where TKey : notnull where TData : notnull
{
    public TKey Id { get; init; } = default!;
    public TData Data { get; init; } = default!;
    public string? Queue { get; init; } = default!;
    public long ScheduledAtMs { get; init; }
    public long CreatedAt { get; init; } = default!;

    object IActionTaskQueueContext.Data => Data;
}