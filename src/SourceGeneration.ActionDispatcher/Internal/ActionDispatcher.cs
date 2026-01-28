using Microsoft.Extensions.DependencyInjection;
using System.Runtime.ExceptionServices;

namespace SourceGeneration.ActionDispatcher.Internal;

internal class ActionDispatcher(IServiceProvider services, IActionNotifier notifier) : IActionDispatcher
{
    public async void Dispatch(object action, ActionDispatchOptions? options, CancellationToken cancellationToken = default) => await DispatchAsync(action, options, false, cancellationToken);

    public Task DispatchAsync(object action, ActionDispatchOptions? options, CancellationToken cancellationToken = default) => DispatchAsync(action, options, true, cancellationToken);

    public void Notify(object action) => notifier.Notify(ActionDispatchStatus.Successed, action);
    public void Notify(object action, Exception exception) => notifier.Notify(action, exception);
    public void Notify(ActionDispatchStatus status, object action, Exception? exception = null) => notifier.Notify(status, action, exception);

    private async Task DispatchAsync(object action, ActionDispatchOptions? options, bool throwException, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action, nameof(action));

        notifier.Notify(ActionDispatchStatus.WaitingToDispatch, action);

        try
        {
            if (options == null || !options.BroadcastOnly)
            {
                await DispatchAsyncCore(action, options, cancellationToken);
            }
        }
        catch (OperationCanceledException ex)
        {
            notifier.Notify(ActionDispatchStatus.Canceled, action, ex);
            return;
        }
        catch (Exception ex)
        {
            notifier.Notify(ActionDispatchStatus.Faulted, action, ex);

            if (throwException)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
            return;
        }

        notifier.Notify(ActionDispatchStatus.Successed, action);
    }

    private Task DispatchAsyncCore(object action, ActionDispatchOptions? options, CancellationToken cancellationToken)
    {
        var actionType = action.GetType();
        var methods = ActionRoutes.GetActionMethod(actionType);

        if (methods.Count == 0)
        {
            options ??= ActionDispatchOptions.Default;

            if (options.ThrowExceptionWhenNotHandled)
            {
                throw new KeyNotFoundException($"No action handler of type '{actionType}' has matched.");
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        Task[] tasks = new Task[methods.Count];

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

            tasks[i] = method.InvokeAsync(arguments);
        }

        return Task.WhenAll(tasks);

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