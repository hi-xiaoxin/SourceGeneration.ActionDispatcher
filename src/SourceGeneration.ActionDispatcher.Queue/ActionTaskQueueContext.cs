namespace SourceGeneration.ActionDispatcher;

public sealed class ActionTaskQueueContext<TAction> : IActionExecutionContext where TAction : notnull
{
    public TAction Action { get; init; } = default!;
    public string? Queue { get; init; } = default!;
    public long ScheduledAtMs { get; init; }
    public long CreatedAt { get; init; } = default!;
    object IActionExecutionContext.Action => Action;
}
