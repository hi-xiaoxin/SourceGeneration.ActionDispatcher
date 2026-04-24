namespace SourceGeneration.ActionDispatcher;

public sealed class ActionTaskQueueContext<TKey, TData> : IActionExecutionContext where TKey : notnull where TData : notnull
{
    public TKey Id { get; init; } = default!;
    public TData Data { get; init; } = default!;
    public string? Queue { get; init; } = default!;
    public long ScheduledAtMs { get; init; }
    public long CreatedAt { get; init; } = default!;
    object IActionExecutionContext.Data => Data;
}
