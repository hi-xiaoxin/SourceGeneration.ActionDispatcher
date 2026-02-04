using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SourceGeneration.ActionDispatcher;

namespace SourceGeneration.ActionDispatcher;

public static class RobosBackgroundTaskServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddBackgroundTaskHandler<T>(Action<BackgroundTaskQueueOptions>? optionsAction = null) where T : notnull
        {
            return services.AddBackgroundTaskQueue<BackgroundTask<T>>(optionsAction);
        }

        public IServiceCollection AddBackgroundTaskQueue<TTask>(Action<BackgroundTaskQueueOptions>? optionsAction = null) where TTask : BackgroundTask
        {
            BackgroundTaskQueueOptions options = new();
            optionsAction?.Invoke(options);

            services.AddSingleton(sp => new BackgroundTaskQueue<TTask>(options,
                sp.GetRequiredService<IBackgroundTaskPersistenceService<TTask>>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<BackgroundTaskQueue<TTask>>>()));

            services.AddSingleton(sp => new ScheduledTaskQueue<TTask>(options,
                sp.GetRequiredService<BackgroundTaskQueue<TTask>>(),
                sp.GetRequiredService<IBackgroundTaskPersistenceService<TTask>>(),
                sp.GetRequiredService<ILogger<ScheduledTaskQueue<TTask>>>()));

            services.AddSingleton<IBackgroundTaskScheduler<TTask>>(sp => sp.GetRequiredService<ScheduledTaskQueue<TTask>>());
            services.AddHostedService(sp => sp.GetRequiredService<BackgroundTaskQueue<TTask>>());
            services.AddHostedService(sp => sp.GetRequiredService<ScheduledTaskQueue<TTask>>());

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
