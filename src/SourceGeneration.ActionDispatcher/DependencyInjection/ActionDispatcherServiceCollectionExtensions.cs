using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SourceGeneration.ActionDispatcher.Internal;
using SourceGeneration.ActionDispatcher.Queue;

namespace SourceGeneration.ActionDispatcher;

public static class ActionDispatcherServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddActionDispatcher(ServiceLifetime dispatcherLifetime = ServiceLifetime.Scoped, ServiceLifetime subscriberLifetime = ServiceLifetime.Singleton)
        {
            services.Add(new ServiceDescriptor(typeof(IActionDispatcher), typeof(DefaultActionDispatcher), dispatcherLifetime));
            services.Add(new ServiceDescriptor(typeof(ActionExecutor), typeof(ActionExecutor), dispatcherLifetime));
            services.TryAdd(new ServiceDescriptor(typeof(ActionSubscriber), typeof(ActionSubscriber), subscriberLifetime));
            services.TryAdd(new ServiceDescriptor(typeof(IActionSubscriber), p => p.GetRequiredService<ActionSubscriber>(), subscriberLifetime));

            services.TryAddSingleton(typeof(IActionScheduledQueue<,>), typeof(NopActionScheduledQueue<,>));

            return services;
        }
    }

    extension(IServiceCollection services)
    {
        public IServiceCollection AddActionQueue<TKey, TData>(Action<ActionQueueOptions>? optionsAction = null)
            where TKey : notnull
            where TData : notnull
        {
            ActionQueueOptions options = new();
            optionsAction?.Invoke(options);

            services.AddSingleton(sp => new ActionQueue<TKey, TData>(options,
                sp.GetRequiredService<ActionSubscriber>(),
                sp.GetRequiredService<IActionPersistenceService<TKey, TData>>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<ActionQueue<TKey, TData>>>()));

            services.AddSingleton(sp => new ActionScheduledQueue<TKey, TData>(options,
                sp.GetRequiredService<ActionSubscriber>(),
                sp.GetRequiredService<ActionQueue<TKey, TData>>(),
                sp.GetRequiredService<IActionPersistenceService<TKey, TData>>(),
                sp.GetRequiredService<ILogger<ActionScheduledQueue<TKey, TData>>>()));

            services.AddSingleton<IActionScheduledQueue<TKey, TData>>(sp => sp.GetRequiredService<ActionScheduledQueue<TKey, TData>>());
            services.AddHostedService(sp => sp.GetRequiredService<ActionQueue<TKey, TData>>());
            services.AddHostedService(sp => sp.GetRequiredService<ActionScheduledQueue<TKey, TData>>());

            services.TryAddSingleton(typeof(IActionPersistenceService<,>), typeof(NopBackgroundTaskPersistenceService<,>));

            return services;
        }

        public IServiceCollection AddActionQueue<TData>(Action<ActionQueueOptions>? optionsAction = null)
            where TData : notnull
        {
            return services.AddActionQueue<Guid,TData>(optionsAction);
        }
    }
}
