namespace SourceGeneration.ActionDispatcher.Internal;

internal class DefaultActionDispatcher(ActionExecutor executor, IServiceProvider services) : IActionDispatcher
{
    public IServiceProvider Services { get; } = services;

    public void Notify(object action)
    {
        executor.Notify(action);
    }

    public void Execute(object action, CancellationToken cancellationToken = default)
    {
        executor.Execute(action, cancellationToken);
    }

    public Task ExecuteAsync(object action, CancellationToken cancellationToken = default)
    {
        return executor.ExecuteAsync(action, cancellationToken);
    }

}
