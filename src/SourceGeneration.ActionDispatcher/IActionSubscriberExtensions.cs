namespace SourceGeneration.ActionDispatcher;

public static class IActionSubscriberExtensions
{
    public static IDisposable Subscribe(this IActionSubscriber actionSubscriber, object? subscriber, DispatchStatus status, Action<object, Exception?> callback) => actionSubscriber.Subscribe<object>(subscriber, status, callback);

    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, object? subscriber, DispatchStatus status, Action<TAction> callback) where TAction : notnull => actionSubscriber.Subscribe(subscriber, status, (TAction action, Exception? ex) => callback(action));
    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, object? subscriber, DispatchStatus status, Action callback) where TAction : notnull => actionSubscriber.Subscribe(subscriber, status, (TAction action, Exception? ex) => callback());

    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, object? subscriber, Action<TAction, Exception?> callback) where TAction : notnull => actionSubscriber.Subscribe(subscriber, DispatchStatus.Completed, callback!);
    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, object? subscriber, Action<TAction> callback) where TAction : notnull => actionSubscriber.Subscribe(subscriber, DispatchStatus.Succeeded, (TAction action, Exception? ex) => callback(action));
    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, object? subscriber, Action callback) where TAction : notnull => actionSubscriber.Subscribe(subscriber, DispatchStatus.Succeeded, (TAction action, Exception? ex) => callback());

    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, DispatchStatus status, Action<TAction, Exception?> callback) where TAction : notnull => actionSubscriber.Subscribe(null, status, callback);
    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, DispatchStatus status, Action<TAction> callback) where TAction : notnull => actionSubscriber.Subscribe(null, status, (TAction action, Exception? ex) => callback(action));
    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, DispatchStatus status, Action callback) where TAction : notnull => actionSubscriber.Subscribe(null, status, (TAction action, Exception? ex) => callback());

    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, Action<TAction, Exception?> callback) where TAction : notnull => actionSubscriber.Subscribe(null, DispatchStatus.Completed, callback!);
    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, Action<TAction> callback) where TAction : notnull => actionSubscriber.Subscribe(null, DispatchStatus.Succeeded, (TAction action, Exception? ex) => callback(action));
    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, Action callback) where TAction : notnull => actionSubscriber.Subscribe(null, DispatchStatus.Succeeded, (TAction action, Exception? ex) => callback());


    public static IDisposable Subscribe(this IActionSubscriber actionSubscriber, object? subscriber, DispatchStatus status, Func<object, Exception?, Task> callback) => actionSubscriber.Subscribe<object>(subscriber, status, callback);

    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, object? subscriber, DispatchStatus status, Func<TAction, Task> callback) where TAction : notnull => actionSubscriber.Subscribe(subscriber, status, (TAction action, Exception? ex) => callback(action));
    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, object? subscriber, DispatchStatus status, Func<Task> callback) where TAction : notnull => actionSubscriber.Subscribe(subscriber, status, (TAction action, Exception? ex) => callback());

    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, object? subscriber, Func<TAction, Exception?, Task> callback) where TAction : notnull => actionSubscriber.Subscribe(subscriber, DispatchStatus.Completed, callback!);
    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, object? subscriber, Func<TAction, Task> callback) where TAction : notnull => actionSubscriber.Subscribe(subscriber, DispatchStatus.Succeeded, (TAction action, Exception? ex) => callback(action));
    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, object? subscriber, Func<Task> callback) where TAction : notnull => actionSubscriber.Subscribe(subscriber, DispatchStatus.Succeeded, (TAction action, Exception? ex) => callback());

    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, DispatchStatus status, Func<TAction, Exception?, Task> callback) where TAction : notnull => actionSubscriber.Subscribe(null, status, callback);
    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, DispatchStatus status, Func<TAction, Task> callback) where TAction : notnull => actionSubscriber.Subscribe(null, status, (TAction action, Exception? ex) => callback(action));
    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, DispatchStatus status, Func<Task> callback) where TAction : notnull => actionSubscriber.Subscribe(null, status, (TAction action, Exception? ex) => callback());

    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, Func<TAction, Exception?, Task> callback) where TAction : notnull => actionSubscriber.Subscribe(null, DispatchStatus.Completed, callback!);
    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, Func<TAction, Task> callback) where TAction : notnull => actionSubscriber.Subscribe(null, DispatchStatus.Succeeded, (TAction action, Exception? ex) => callback(action));
    public static IDisposable Subscribe<TAction>(this IActionSubscriber actionSubscriber, Func<Task> callback) where TAction : notnull => actionSubscriber.Subscribe(null, DispatchStatus.Succeeded, (TAction action, Exception? ex) => callback());

}
