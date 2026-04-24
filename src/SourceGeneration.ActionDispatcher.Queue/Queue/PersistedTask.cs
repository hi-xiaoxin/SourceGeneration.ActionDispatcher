namespace SourceGeneration.ActionDispatcher.Queue;

public class PersistedTask<TKey, TAction> where TKey : notnull where TAction : notnull
{
    public TKey Id { get; set; } = default!;
    public string? Queue { get; set; }
    public TAction Data { get; set; } = default!;
    public long ScheduledAtMs { get; set; }
    public long CreatedAt { get; set; }
}