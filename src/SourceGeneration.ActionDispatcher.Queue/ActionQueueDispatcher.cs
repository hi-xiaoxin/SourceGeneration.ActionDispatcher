using Microsoft.Extensions.DependencyInjection;
using SourceGeneration.ActionDispatcher.Queue;

namespace SourceGeneration.ActionDispatcher;

public static class ActionQueueDispatcher
{
    extension(IActionDispatcher dispatcher)
    {
        public void Cancel<TAction>(object id) where TAction : notnull
        {
            dispatcher.Services.GetRequiredService<IActionScheduledQueue<TAction>>().Cancel(id);
        }

        public ValueTask EnqueueAsync<TAction>(TAction action) where TAction : notnull => dispatcher.ScheduleAsync(action, scheduledMs: 0);
        public ValueTask EnqueueAsync<TAction>(TAction[] actions) where TAction : notnull => dispatcher.ScheduleAsync(actions, scheduledMs: 0);

        public ValueTask ScheduleAsync<TAction>(TAction action, DateTimeOffset scheduledAt) where TAction : notnull
        {
            return dispatcher.Services.GetRequiredService<IActionScheduledQueue<TAction>>().ScheduleAsync([action], scheduledAt.ToUnixTimeMilliseconds());
        }

        public ValueTask ScheduleAsync<TAction>(TAction[] actions, DateTimeOffset scheduledAt) where TAction : notnull
        {
            return dispatcher.Services.GetRequiredService<IActionScheduledQueue<TAction>>().ScheduleAsync(actions, scheduledAt.ToUnixTimeMilliseconds());
        }

        public ValueTask ScheduleAsync<TAction>(TAction action, long scheduledMs = 0) where TAction : notnull
        {
            return dispatcher.Services.GetRequiredService<IActionScheduledQueue<TAction>>().ScheduleAsync([action], scheduledMs);
        }

        public ValueTask ScheduleAsync<TAction>(TAction[] actions, long scheduledMs = 0) where TAction : notnull
        {
            return dispatcher.Services.GetRequiredService<IActionScheduledQueue<TAction>>().ScheduleAsync(actions, scheduledMs);
        }
    }
}