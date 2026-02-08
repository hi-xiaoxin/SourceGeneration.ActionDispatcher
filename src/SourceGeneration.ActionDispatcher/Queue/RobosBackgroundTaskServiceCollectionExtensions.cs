using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SourceGeneration.ActionDispatcher;
using SourceGeneration.ActionDispatcher.Internal;

namespace SourceGeneration.ActionDispatcher;

public static class RobosBackgroundTaskServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {

        public IServiceCollection AddActionTaskQueue<T>(Action<BackgroundTaskQueueOptions>? optionsAction = null) where T : notnull
        {
            BackgroundTaskQueueOptions options = new();
            optionsAction?.Invoke(options);

            services.AddSingleton(sp => new BackgroundTaskQueue<T>(options,
                sp.GetRequiredService<ActionSubscriber>(),
                sp.GetRequiredService<IBackgroundTaskPersistenceService<T>>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<BackgroundTaskQueue<T>>>()));

            services.AddSingleton(sp => new ScheduledTaskQueue<T>(options,
                sp.GetRequiredService<ActionSubscriber>(),
                sp.GetRequiredService<BackgroundTaskQueue<T>>(),
                sp.GetRequiredService<IBackgroundTaskPersistenceService<T>>(),
                sp.GetRequiredService<ILogger<ScheduledTaskQueue<T>>>()));

            //services.AddSingleton<IBackgroundTaskScheduler<TTask>>(sp => sp.GetRequiredService<ScheduledTaskQueue<TTask>>());
            services.AddHostedService(sp => sp.GetRequiredService<BackgroundTaskQueue<T>>());
            services.AddHostedService(sp => sp.GetRequiredService<ScheduledTaskQueue<T>>());

            services.TryAddSingleton(typeof(IBackgroundTaskPersistenceService<>), typeof(NopBackgroundTaskPersistenceService<>));

            return services;
        }

        //public IServiceCollection AddBackgroundTaskEFCoreStore(Action<DbContextOptionsBuilder> optionsAction)
        //{
        //    services.AddDbContextFactory<BackgroundTaskDbContext>(optionsAction, ServiceLifetime.Singleton);
        //    services.AddSingleton(typeof(IBackgroundTaskPersistenceService<>), typeof(EFCoreBackgroundTaskPersistenceService<>));
        //    return services;
        //}
    }
}
