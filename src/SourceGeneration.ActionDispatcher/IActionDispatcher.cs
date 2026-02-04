namespace SourceGeneration.ActionDispatcher;

public interface IActionDispatcher
{
    ValueTask EnqueueAsync<T>(T action) where T : notnull;
    ValueTask ScheduleAsync<T>(T action, DateTimeOffset scheduledTime) where T : notnull;
    void Execute(object action, CancellationToken cancellationToken = default);
    Task ExecuteAsync(object action, CancellationToken cancellationToken = default);
}
