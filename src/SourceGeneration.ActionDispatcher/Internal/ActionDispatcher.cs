using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace SourceGeneration.ActionDispatcher.Internal;

internal class DefaultActionDispatcher(ActionExecutor executor, IServiceProvider services) : IActionDispatcher
{
    public void Notify(object action) => executor.Notify(action);

    public ValueTask ScheduleAsync<T>(T action, long scheduledAtMs = 0, long businessId = 0)
    {
        return services.GetRequiredService<ScheduledTaskQueue<T>>().ScheduleAsync([new DispatchItem<T>
        {
            Data = action,
            BusinessId = businessId
        }], scheduledAtMs);
    }

    public ValueTask ScheduleAsync<T>(IEnumerable<T> actions, long scheduledAtMs = 0)
    {
        return services.GetRequiredService<ScheduledTaskQueue<T>>().ScheduleAsync([.. actions.Select(x => new DispatchItem<T>
        {
            Data = x,
        })], scheduledAtMs);
    }

    public ValueTask ScheduleAsync<T>(IEnumerable<DispatchItem<T>> items, long scheduledAtMs = 0)
    {
        return services.GetRequiredService<ScheduledTaskQueue<T>>().ScheduleAsync([.. items], scheduledAtMs);
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
