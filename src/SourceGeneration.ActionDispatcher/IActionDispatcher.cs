namespace SourceGeneration.ActionDispatcher;

public interface IActionDispatcher
{
    void Notify(object action);

    void Execute(object action, CancellationToken cancellationToken = default);
    Task ExecuteAsync(object action, CancellationToken cancellationToken = default);

    ValueTask<Guid> EnqueueAsync<TData>(TData action)
        where TData : notnull
        => ScheduleAsync(action, scheduledAtMs: 0);

    ValueTask<Guid[]> EnqueueAsync<TData>(TData[] actions)
        where TData : notnull
        => ScheduleAsync(actions, scheduledAtMs: 0);

    ValueTask EnqueueAsync<TKey, TData>(TKey businessId, TData action)
        where TKey : notnull
        where TData : notnull
        => ScheduleAsync(businessId, action, scheduledAtMs: 0);

    ValueTask EnqueueAsync<TKey, TData>(IEnumerable<DispatchItem<TKey, TData>> items)
        where TKey : notnull
        where TData : notnull
        => ScheduleAsync(items, scheduledAtMs: 0);

    ValueTask ScheduleAsync<TKey, TData>(TKey businessId, TData action, long scheduledAtMs)
        where TKey : notnull
        where TData : notnull;

    ValueTask ScheduleAsync<TKey, TData>(IEnumerable<DispatchItem<TKey, TData>> items, DateTimeOffset? scheduledAt)
        where TKey : notnull
        where TData : notnull
        => ScheduleAsync(items, scheduledAt?.ToUnixTimeMilliseconds() ?? 0);

    ValueTask<Guid> ScheduleAsync<TData>(TData action, DateTimeOffset? scheduledAt)
        where TData : notnull
        => ScheduleAsync(action, scheduledAt?.ToUnixTimeMilliseconds() ?? 0);

    ValueTask<Guid> ScheduleAsync<TData>(TData action, long scheduledAtMs = 0)
        where TData : notnull;

    ValueTask<Guid[]> ScheduleAsync<TData>(TData[] actions, long scheduledAtMs) 
        where TData : notnull;

    ValueTask ScheduleAsync<TKey, TData>(IEnumerable<DispatchItem<TKey, TData>> items, long scheduledAtMs)
        where TKey : notnull
        where TData : notnull;

    void Cancel<TKey, TData>(TKey id) where TKey : notnull where TData : notnull;
}

public class DispatchItem<TKey, TData> where TKey : notnull where TData : notnull
{
    public TData Data { get; set; } = default!;
    public TKey Id { get; set; } = default!;
}