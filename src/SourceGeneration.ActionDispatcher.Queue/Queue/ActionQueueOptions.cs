namespace SourceGeneration.ActionDispatcher.Queue;

public class ActionQueueOptions<TAction>
{
    public string? QueueName { get; set; }
    public bool IsPersisted { get; set; } = true;
    public int MaxConcurrency { get; set; }
    public Func<TAction, object>? KeySelector { get; set; }
}
