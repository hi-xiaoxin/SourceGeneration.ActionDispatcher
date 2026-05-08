namespace SourceGeneration.ActionDispatcher.Queue;

public class ActionTask<TAction> where TAction : notnull
{
    public object Key { get; init; } = null!;
    public string? Queue { get; init; }
    public TAction Action { get; init; } = default!;
    public long ScheduledMs { get; init; }
    public long CreatedMs { get; init; }
}