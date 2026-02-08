using System;
using System.Collections.Generic;
using System.Text;

namespace SourceGeneration.ActionDispatcher.Test;


internal interface IHandler
{
    [ActionHandler] void Handle(Action1 action);
    //[ActionHandler] void Handle2(Action1 action);
}

internal class Handler : IHandler
{
    public void Handle(Action1 action)
    {
        action.Result++;
    }

    public void Handle2(Action1 action)
    {
        action.Result++;
    }

}
