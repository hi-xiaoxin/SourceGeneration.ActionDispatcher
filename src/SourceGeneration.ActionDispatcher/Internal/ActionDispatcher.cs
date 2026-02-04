using Microsoft.Extensions.DependencyInjection;

namespace SourceGeneration.ActionDispatcher.Internal;

internal class DefaultActionDispatcher(ActionExecutor executor, IServiceProvider services) : IActionDispatcher
{
    public ValueTask EnqueueAsync<T>(T action) where T : notnull
    {
        return services.GetRequiredService<BackgroundTaskQueue<BackgroundTask<T>>>().EnqueueAsync([new BackgroundTask<T> { Data = action }]);
    }

    public ValueTask ScheduleAsync<T>(T action, DateTimeOffset scheduledTime) where T : notnull
    {
        return services.GetRequiredService<ScheduledTaskQueue<BackgroundTask<T>>>().ScheduleAsync([new BackgroundTask<T> { Data = action }], scheduledTime.ToUnixTimeMilliseconds());
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
