namespace SourceGeneration.ActionDispatcher.Queue;

public class ActionQueueTask<TAction> where TAction : notnull
{
    public object Key { get; set; } = null!;
    public string? Queue { get; init; }
    public TAction Action { get; init; } = default!;
    public long ScheduledMs { get; init; }
    public long CreatedMs { get; init; }
}