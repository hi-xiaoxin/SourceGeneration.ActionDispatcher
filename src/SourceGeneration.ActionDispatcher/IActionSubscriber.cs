namespace SourceGeneration.ActionDispatcher;

public interface IActionSubscriber
{
    void Unsubscribe(object subscriber);

    IDisposable Subscribe<TAction>(object? subscriber, DispatchStatus status, Action<TAction, Exception?> callback) where TAction : notnull;
    IDisposable Subscribe(object? subscriber, DispatchStatus status, Type[] actionTypes, Action<object, Exception?> callback);

    IDisposable Subscribe<TAction>(object? subscriber, DispatchStatus status, Func<TAction, Exception?, Task> callback) where TAction : notnull;
    IDisposable Subscribe(object? subscriber, DispatchStatus status, Type[] actionTypes, Func<object, Exception?, Task> callback);

}