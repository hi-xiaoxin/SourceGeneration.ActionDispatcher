using Microsoft.Extensions.Logging;
using SourceGeneration.ActionDispatcher.Queue;

namespace SourceGeneration.ActionDispatcher.Test;

internal interface IHandler
{
    [ActionHandler]
    void Handle(ActionTaskQueueContext<Guid, Action1> action);
}

internal class Handler(ILogger<Handler> logger, IServiceProvider services,IActionDispatcher dispatcher) : IHandler
{
    public void Handle(ActionTaskQueueContext<Guid, Action1> action)
    {
        var s = services;
        var d= dispatcher;
        action.Data.Result++;

        logger.LogInformation("handled");
    }
}
