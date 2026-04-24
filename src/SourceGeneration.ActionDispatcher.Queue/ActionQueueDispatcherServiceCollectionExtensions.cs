using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SourceGeneration.ActionDispatcher.Internal;
using SourceGeneration.ActionDispatcher.Queue;

namespace SourceGeneration.ActionDispatcher;

public static class ActionQueueDispatcherServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddActionQueue<TAction>(Action<ActionQueueOptions<TAction>>? optionsAction = null) where TAction : notnull
        {
            ActionQueueOptions<TAction> options = new();
            optionsAction?.Invoke(options);

            services.AddSingleton(sp => new ActionQueue<TAction>(options,
                sp.GetRequiredService<ActionSubscriber>(),
                sp.GetRequiredService<IActionPersistenceService<TAction>>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<ActionQueue<TAction>>>()));

            services.AddSingleton(sp => new ActionScheduledQueue<TAction>(options,
                sp.GetRequiredService<ActionSubscriber>(),
                sp.GetRequiredService<ActionQueue<TAction>>(),
                sp.GetRequiredService<IActionPersistenceService<TAction>>(),
                sp.GetRequiredService<ILogger<ActionScheduledQueue<TAction>>>()));

            services.AddSingleton<IActionScheduledQueue<TAction>>(sp => sp.GetRequiredService<ActionScheduledQueue<TAction>>());
            services.AddHostedService(sp => sp.GetRequiredService<ActionQueue<TAction>>());
            services.AddHostedService(sp => sp.GetRequiredService<ActionScheduledQueue<TAction>>());

            services.TryAddSingleton<IActionPersistenceService<TAction>, NopBackgroundTaskPersistenceService<TAction>>();

            return services;
        }
    }
}
