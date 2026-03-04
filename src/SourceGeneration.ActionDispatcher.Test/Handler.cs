using SourceGeneration.ActionDispatcher.Queue;

namespace SourceGeneration.ActionDispatcher.Test;

internal interface IHandler
{
    [ActionHandler] void Handle(ActionTaskQueueContext<Guid, Action1> action);
}

internal class Handler : IHandler
{
    public void Handle(ActionTaskQueueContext<Guid, Action1> action)
    {
        action.Data.Result++;
    }
}
