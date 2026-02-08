using Microsoft.Extensions.Logging;
using System.Collections.Immutable;

namespace SourceGeneration.ActionDispatcher.Internal;

internal class ActionSubscriber(ILogger<ActionSubscriber> logger) : IActionSubscriber
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private ImmutableArray<SubscriptionBase> _subscriptions = [];

    public void Notify(DispatchStatus status, object action, Exception? exception = null)
    {
        var subscribes = _subscriptions;
        var type = action.GetType();
        foreach (var subscription in subscribes.Where(x => x.IsMatch(type, status)))
        {
            try
            {
                subscription.Notify(action!, exception);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Somethine wrong when notify action");
            }
        }
    }

    public IDisposable Subscribe<TAction>(object? subscriber, DispatchStatus status, Action<TAction, Exception?> callback) where TAction : notnull
    {
        lock (_lock)
        {
            var subscription = new Subscription<TAction>(subscriber, status, callback);
            _subscriptions = _subscriptions.Add(subscription);
            return new Disposable(() => { lock (_lock) _subscriptions = _subscriptions.Remove(subscription); });
        }
    }

    public IDisposable Subscribe(object? subscriber, DispatchStatus status, Type[] actionTypes, Action<object, Exception?> callback)
    {
        lock (_lock)
        {
            var subscription = new Subscription(subscriber, status, actionTypes, callback);
            _subscriptions = _subscriptions.Add(subscription);
            return new Disposable(() => { lock (_lock) _subscriptions = _subscriptions.Remove(subscription); });
        }
    }

    public IDisposable Subscribe<TAction>(object? subscriber, DispatchStatus status, Func<TAction, Exception?, Task> callback) where TAction : notnull
    {
        lock (_lock)
        {
            var subscription = new AsyncSubscription<TAction>(subscriber, status, callback, logger);
            _subscriptions = _subscriptions.Add(subscription);
            return new Disposable(() => { lock (_lock) _subscriptions = _subscriptions.Remove(subscription); });
        }
    }

    public IDisposable Subscribe(object? subscriber, DispatchStatus status, Type[] actionTypes, Func<object, Exception?, Task> callback)
    {
        lock (_lock)
        {
            var subscription = new AsyncSubscription(subscriber, status, actionTypes, callback, logger);
            _subscriptions = _subscriptions.Add(subscription);
            return new Disposable(() => { lock (_lock) _subscriptions = _subscriptions.Remove(subscription); });
        }
    }

    public void Unsubscribe(object subscriber)
    {
        lock (_lock)
        {
            _subscriptions = _subscriptions.RemoveRange(_subscriptions.Where(x => Equals(x.Subscriber, subscriber)));
        }
    }

    private abstract class SubscriptionBase(object? subscriber, DispatchStatus subscribeStatus, Type[] actionType)
    {
        public readonly object? Subscriber = subscriber;
        public bool IsMatch(Type type, DispatchStatus status) => subscribeStatus.HasFlag(status) && actionType.Any(x => type.IsAssignableTo(x));

        public abstract void Notify(object action, Exception? exception);
    }

    private sealed class Subscription(object? subscriber, DispatchStatus subscribeStatus, Type[] actionType, Action<object, Exception?> callback) : SubscriptionBase(subscriber, subscribeStatus, actionType)
    {
        public override void Notify(object action, Exception? exception) => callback(action, exception);
    }

    private sealed class Subscription<T>(object? subscriber, DispatchStatus subscribeStatus, Action<T, Exception?> callback) : SubscriptionBase(subscriber, subscribeStatus, [typeof(T)])
    {
        public override void Notify(object action, Exception? exception) => callback((T)action, exception);
    }

    private sealed class AsyncSubscription(object? subscriber, DispatchStatus subscribeStatus, Type[] actionType, Func<object, Exception?, Task> callback, ILogger logger) : SubscriptionBase(subscriber, subscribeStatus, actionType)
    {
        public override async void Notify(object action, Exception? exception)
        {
            try
            {
                await callback(action, exception).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Somethine wrong when notify action");
            }
        }
    }

    private sealed class AsyncSubscription<T>(object? subscriber, DispatchStatus subscribeStatus, Func<T, Exception?, Task> callback, ILogger logger) : SubscriptionBase(subscriber, subscribeStatus, [typeof(T)])
    {
        public override async void Notify(object action, Exception? exception)
        {
            try
            {
                await callback((T)action, exception).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Somethine wrong when notify action");
            }
        }
    }

    private sealed class Disposable(Action action) : IDisposable
    {
        public void Dispose() => action();
    }
}
