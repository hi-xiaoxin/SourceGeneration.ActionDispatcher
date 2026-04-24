namespace SourceGeneration.ActionDispatcher.Queue;

public class PersistedTask<TAction> where TAction : notnull
{
    public string? Queue { get; set; }
    public TAction Action { get; set; } = default!;
    public long ScheduledMs { get; set; }
    public long CreatedMs { get; set; }
}