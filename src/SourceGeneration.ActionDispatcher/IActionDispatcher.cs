using System;

namespace SourceGeneration.ActionDispatcher;

public interface IActionDispatcher
{
    void Notify(object action);

    void Execute(object action, CancellationToken cancellationToken = default);
    Task ExecuteAsync(object action, CancellationToken cancellationToken = default);

    ValueTask EnqueueAsync<T>(T action, long businessId = 0) => ScheduleAsync(action, scheduledAtMs: 0, businessId: businessId);
    ValueTask EnqueueAsync<T>(IEnumerable<T> actions) => ScheduleAsync(actions, scheduledAtMs: 0);
    ValueTask EnqueueAsync<T>(IEnumerable<DispatchItem<T>> items) => ScheduleAsync(items, scheduledAtMs: 0);

    ValueTask ScheduleAsync<T>(T action, long scheduledAtMs = 0, long businessId = 0);
    ValueTask ScheduleAsync<T>(IEnumerable<T> actions, long scheduledAtMs = 0);
    ValueTask ScheduleAsync<T>(IEnumerable<DispatchItem<T>> items, long scheduledAtMs = 0);

    ValueTask ScheduleAsync<T>(T action, DateTimeOffset? scheduledAt = null, long businessId = 0) => ScheduleAsync(action, scheduledAt?.ToUnixTimeMilliseconds() ?? 0, businessId);
    ValueTask ScheduleAsync<T>(IEnumerable<T> actions, DateTimeOffset? scheduledAt = null) => ScheduleAsync(actions, scheduledAt?.ToUnixTimeMilliseconds() ?? 0);
    ValueTask ScheduleAsync<T>(IEnumerable<DispatchItem<T>> items, DateTimeOffset? scheduledAt = null) => ScheduleAsync(items, scheduledAt?.ToUnixTimeMilliseconds() ?? 0);
}

public class DispatchItem<T>
{
    public T Data { get; set; } = default!;
    public long BusinessId { get; set; }
}