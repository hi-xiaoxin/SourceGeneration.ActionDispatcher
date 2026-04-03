using Microsoft.Extensions.DependencyInjection;
using SourceGeneration.ActionDispatcher.Queue;

namespace SourceGeneration.ActionDispatcher.Internal;

internal class DefaultActionDispatcher(ActionExecutor executor, IServiceProvider services) : IActionDispatcher
{
    public void Notify(object action) => executor.Notify(action);

    public void Cancel<TKey, TData>(TKey id)
        where TKey : notnull
        where TData : notnull
    {
        services.GetRequiredService<IActionScheduledQueue<TKey, TData>>().Cancel(id);
    }

    public async ValueTask<Guid> ScheduleAsync<TData>(TData action, long scheduledAtMs = 0)
        where TData : notnull
    {
#if (NET9_0_OR_GREATER)
        Guid id = Guid.CreateVersion7();
#else
        Guid id = Guid.NewGuid();
#endif
        await services.GetRequiredService<IActionScheduledQueue<Guid, TData>>().ScheduleAsync([new DispatchItem<Guid, TData>
        {
            Id = id,
            Data = action
        }], scheduledAtMs).ConfigureAwait(false);

        return id;
    }

    public async ValueTask<Guid[]> ScheduleAsync<TData>(TData[] actions, long scheduledAtMs = 0)
        where TData : notnull
    {
        Guid[] ids = new Guid[actions.Length];
        DispatchItem<Guid, TData>[] items = new DispatchItem<Guid, TData>[actions.Length];
        for(int i = 0;i< actions.Length; i++)
        {
#if (NET9_0_OR_GREATER)
            Guid id = Guid.CreateVersion7();
#else
            Guid id = Guid.NewGuid();
#endif
            ids[i] = id;
            items[i] = new DispatchItem<Guid, TData>
            {
                Id = id,
                Data = actions[i],
            };

        }

        await services.GetRequiredService<IActionScheduledQueue<Guid, TData>>().ScheduleAsync(items, scheduledAtMs).ConfigureAwait(false);

        return ids;
    }

    public ValueTask ScheduleAsync<TKey, TData>(TKey id, TData action, long scheduledAtMs = 0)
        where TKey : notnull
        where TData : notnull
    {
        return services.GetRequiredService<IActionScheduledQueue<TKey, TData>>().ScheduleAsync([new DispatchItem<TKey, TData>
        {
            Id = id,
            Data = action
        }], scheduledAtMs);
    }

    public ValueTask ScheduleAsync<TKey, TData>(IEnumerable<DispatchItem<TKey, TData>> items, long scheduledAtMs = 0)
        where TKey : notnull
        where TData : notnull
    {
        return services.GetRequiredService<IActionScheduledQueue<TKey, TData>>().ScheduleAsync([.. items], scheduledAtMs);
    }

    public void Execute(object action, CancellationToken cancellationToken = default)
    {
        executor.Execute(action, cancellationToken);
    }

    public Task ExecuteAsync(object action, CancellationToken cancellationToken = default)
    {
        return executor.ExecuteAsync(action, cancellationToken);
    }

}
