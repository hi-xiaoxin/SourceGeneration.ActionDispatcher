using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.ExceptionServices;

namespace SourceGeneration.ActionDispatcher.Internal;

internal class ActionExecutor(IServiceProvider services, ActionSubscriber notifier)
{
    public void Notify(object action) => notifier.Notify(DispatchStatus.Completed, action);

    public async void Execute(object action, CancellationToken cancellationToken = default) => await InternalExecuteAsync(action, false, cancellationToken).ConfigureAwait(false);

    public Task ExecuteAsync(object action, CancellationToken cancellationToken = default) => InternalExecuteAsync(action, true, cancellationToken);


    private async Task InternalExecuteAsync(object action, bool throwException, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action, nameof(action));

        notifier.Notify(DispatchStatus.Running, action);

        try
        {
            await ExecuteCoreAsync(action, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            notifier.Notify(DispatchStatus.Canceled, action, ex);
            return;
        }
        catch (Exception ex)
        {
            notifier.Notify(DispatchStatus.Faulted, action, ex);

            if (throwException)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
            return;
        }

        notifier.Notify(DispatchStatus.Succeeded, action);
    }

    private async Task ExecuteCoreAsync(object action, CancellationToken cancellationToken)
    {
        var actionType = action.GetType();
        var methods = ActionRoutes.GetActionMethod(actionType);

        if (methods.Count == 0)
        {
            return;
        }

        Dictionary<Type, object> injects = [];

        for (int i = 0; i < methods.Count; i++)
        {
            ActionMethod method = methods[i];
            var parameters = method.Parameters;
            object?[] arguments = new object?[parameters.Length];

            arguments[0] = parameters[0] == null ? null : CreateInstance(parameters[0]);
            arguments[1] = action;

            for (int j = 2; j < parameters.Length; j++)
            {
                var type = parameters[j];
                if (type == typeof(CancellationToken))
                {
                    arguments[j] = cancellationToken;
                }
                else
                {
                    arguments[j] = GetRequiredService(type);
                }
            }

            await method.InvokeAsync(arguments).ConfigureAwait(false);
        }

        object GetRequiredService(Type serviceType)
        {
            if (injects.TryGetValue(serviceType, out var service))
                return service;

            service = services.GetRequiredService(serviceType);
            injects.Add(serviceType, service);

            return service;
        }

        object CreateInstance(Type instanceType)
        {
            if (injects.TryGetValue(instanceType, out var instance))
                return instance;

            instance = services.GetService(instanceType);
            if (instance == null)
            {
                var definition = ActionRoutes.GetActionDeclaringTypeConstructor(instanceType);

                object[] arguments = new object[definition.Parameters.Length];
                for (int i = 0; i < arguments.Length; i++)
                {
                    arguments[i] = GetRequiredService(definition.Parameters[i]);
                }
                instance = definition.InvokeAsync(arguments);
            }

            injects.Add(instanceType, instance);
            return instance;
        }

    }

}