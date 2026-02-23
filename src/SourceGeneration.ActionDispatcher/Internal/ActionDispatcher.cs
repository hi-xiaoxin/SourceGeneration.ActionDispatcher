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
