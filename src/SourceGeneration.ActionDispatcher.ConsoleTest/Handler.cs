using Microsoft.Extensions.Logging;

namespace SourceGeneration.ActionDispatcher.Test;

internal interface IHandler
{
    [ActionHandler]
    void Handle(ActionTaskQueueContext<Action1> action);
}

internal class Handler(ILogger<Handler> logger, IServiceProvider services,IActionDispatcher dispatcher) : IHandler
{
    public void Handle(ActionTaskQueueContext<Action1> action)
    {
        var s = services;
        var d= dispatcher;
        action.Action.Result++;

        logger.LogInformation("handled");
    }
}
