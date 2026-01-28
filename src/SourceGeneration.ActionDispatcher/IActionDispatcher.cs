namespace SourceGeneration.ActionDispatcher;

public class ActionDispatchOptions
{
    public bool ThrowExceptionWhenNotHandled { get; set; } = true;
    public bool BroadcastOnly { get; set; }

    internal static readonly ActionDispatchOptions Default = new();
    internal static readonly ActionDispatchOptions Broadcast = new() { ThrowExceptionWhenNotHandled = false, BroadcastOnly = true };
}

public interface IActionDispatcher: IActionNotifier
{
    void Dispatch(object action, ActionDispatchOptions? options, CancellationToken cancellationToken = default);
    Task DispatchAsync(object action, ActionDispatchOptions? options, CancellationToken cancellationToken = default);

    //void Dispatch(object action, CancellationToken cancellationToken = default) => Dispatch(action, null, cancellationToken);
    //Task DispatchAsync(object action, CancellationToken cancellationToken = default) => DispatchAsync(action, null, cancellationToken);

    //void Dispatch<TAction>(CancellationToken cancellationToken = default) where TAction : new() => Dispatch(new TAction(), null, cancellationToken);
    //Task DispatchAsync<TAction>(CancellationToken cancellationToken = default) where TAction : new() => DispatchAsync(new TAction(), null, cancellationToken);

    //void Dispatch<TAction>(ActionDispatchOptions? options, CancellationToken cancellationToken = default) where TAction : new() => Dispatch(new TAction(), options, cancellationToken);
    //Task DispatchAsync<TAction>(ActionDispatchOptions? options, CancellationToken cancellationToken = default) where TAction : new() => DispatchAsync(new TAction(), options, cancellationToken);
}
