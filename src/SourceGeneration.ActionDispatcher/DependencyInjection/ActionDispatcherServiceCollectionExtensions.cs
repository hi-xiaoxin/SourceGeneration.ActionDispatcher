using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SourceGeneration.ActionDispatcher.Internal;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("SourceGeneration.ActionDispatcher.Queue")]

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

            //services.TryAddSingleton(typeof(IActionScheduledQueue<,>), typeof(NopActionScheduledQueue<,>));

            return services;
        }
    }
}
