namespace SourceGeneration.ActionDispatcher;

public class PersistedTask<T>
{
    public Guid Id { get; set; }
    public long BusinessId { get; set; }
    public string? Queue { get; set; }
    public T Data { get; set; } = default!;
    public long ScheduledAtMs { get; set; }
    public long CreatedAt { get; set; }
}