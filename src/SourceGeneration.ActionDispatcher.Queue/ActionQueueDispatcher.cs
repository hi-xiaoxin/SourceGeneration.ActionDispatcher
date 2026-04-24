using Microsoft.Extensions.DependencyInjection;
using SourceGeneration.ActionDispatcher.Queue;

namespace SourceGeneration.ActionDispatcher;

public static class ActionQueueDispatcher
{
    extension(IActionDispatcher dispatcher)
    {
        public void Cancel<TData>(Guid id) where TData : notnull
        {
            dispatcher.Services.GetRequiredService<IActionScheduledQueue<Guid, TData>>().Cancel(id);
        }

        public void Cancel<TKey, TData>(TKey id) where TKey : notnull where TData : notnull
        {
            dispatcher.Services.GetRequiredService<IActionScheduledQueue<TKey, TData>>().Cancel(id);
        }

        public ValueTask<Guid> EnqueueAsync<TData>(TData action) where TData : notnull => dispatcher.ScheduleAsync(action, scheduledAtMs: 0);
        public ValueTask<Guid[]> EnqueueAsync<TData>(TData[] actions) where TData : notnull => dispatcher.ScheduleAsync(actions, scheduledAtMs: 0);
        public ValueTask EnqueueAsync<TKey, TData>(TKey businessId, TData action) where TKey : notnull where TData : notnull => dispatcher.ScheduleAsync(businessId, action, scheduledAtMs: 0);
        public ValueTask EnqueueAsync<TKey, TData>(IEnumerable<DispatchItem<TKey, TData>> items) where TKey : notnull where TData : notnull => dispatcher.ScheduleAsync(items, scheduledAtMs: 0);


        public async ValueTask<Guid> ScheduleAsync<TData>(TData action, long scheduledAtMs = 0) where TData : notnull
        {
            Guid id = CreateGuid();
            await dispatcher.Services.GetRequiredService<IActionScheduledQueue<Guid, TData>>().ScheduleAsync([new DispatchItem<Guid, TData>
            {
                Id = id,
                Data = action
            }], scheduledAtMs).ConfigureAwait(false);

            return id;
        }

        public async ValueTask<Guid[]> ScheduleAsync<TData>(TData[] actions, long scheduledAtMs = 0) where TData : notnull
        {
            Guid[] ids = new Guid[actions.Length];
            DispatchItem<Guid, TData>[] items = new DispatchItem<Guid, TData>[actions.Length];

            for (int i = 0; i < actions.Length; i++)
            {
                Guid id = CreateGuid();
                ids[i] = id;
                items[i] = new DispatchItem<Guid, TData>
                {
                    Id = id,
                    Data = actions[i],
                };
            }

            await dispatcher.Services.GetRequiredService<IActionScheduledQueue<Guid, TData>>().ScheduleAsync(items, scheduledAtMs).ConfigureAwait(false);

            return ids;
        }

        public ValueTask ScheduleAsync<TKey, TData>(TKey id, TData action, long scheduledAtMs = 0) where TKey : notnull where TData : notnull
        {
            return dispatcher.Services.GetRequiredService<IActionScheduledQueue<TKey, TData>>().ScheduleAsync([new DispatchItem<TKey, TData>
            {
                Id = id,
                Data = action
            }], scheduledAtMs);
        }

        public ValueTask ScheduleAsync<TKey, TData>(IEnumerable<DispatchItem<TKey, TData>> items, DateTimeOffset? scheduledAt) where TKey : notnull where TData : notnull
        {
            return dispatcher.ScheduleAsync(items, scheduledAt?.ToUnixTimeMilliseconds() ?? 0);
        }

        public ValueTask ScheduleAsync<TKey, TData>(IEnumerable<DispatchItem<TKey, TData>> items, long scheduledAtMs = 0) where TKey : notnull where TData : notnull
        {
            return dispatcher.Services.GetRequiredService<IActionScheduledQueue<TKey, TData>>().ScheduleAsync([.. items], scheduledAtMs);
        }
    }

    private static Guid CreateGuid()
    {
#if (NET9_0_OR_GREATER)
        return Guid.CreateVersion7();
#else
        return Guid.NewGuid();
#endif
    }
}

public class DispatchItem<TKey, TData> where TKey : notnull where TData : notnull
{
    public TData Data { get; set; } = default!;
    public TKey Id { get; set; } = default!;
}