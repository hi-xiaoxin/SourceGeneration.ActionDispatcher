namespace SourceGeneration.ActionDispatcher.Queue;

public class PersistedActionTask<TAction> where TAction : notnull
{
    public object? Key { get; set; }
    public string? Queue { get; set; }
    public TAction Action { get; set; } = default!;
    public long ScheduledMs { get; set; }
    public long CreatedMs { get; set; }
}