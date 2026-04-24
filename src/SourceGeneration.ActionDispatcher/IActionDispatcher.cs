namespace SourceGeneration.ActionDispatcher;

public interface IActionDispatcher
{
    IServiceProvider Services { get; }
    void Notify(object action);
    void Execute(object action, CancellationToken cancellationToken = default);
    Task ExecuteAsync(object action, CancellationToken cancellationToken = default);
}