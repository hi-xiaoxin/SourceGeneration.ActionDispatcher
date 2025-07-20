# ActionDispatcher

[![NuGet](https://img.shields.io/nuget/vpre/SourceGeneration.ActionDispatcher.svg)](https://www.nuget.org/packages/SourceGeneration.ActionDispatcher)

Source generator based mediator implementation without the reflection `AOTable`. 
Supports action dispatch and subscribe.

## Installing ActionDispatcher

```powershell
Install-Package SourceGeneration.ActionDispatcher -Version 1.0.0
```

```powershell
dotnet add package SourceGeneration.ActionDispatcher --version 1.0.0
```

## Registering with IServiceCollection

```c#
services.AddActionDispatcher()
```

## `Action` or `Message` or `Command` or `Event`

```c#
public class Say
{
    public string? Text { get; set; }
}
```

## Handler

You just need add `ActionHandlerAttribute` to method
```c#
public class ActionHandler
{
    [ActionHandler]
    public void Handle(Say say, ILogger<ActionHandler> logger, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handled : " + say.Text);
    }

    [ActionHandler]
    public static void StaticHandle(Say say, ILogger<ActionHandler> logger, CancellationToken cancellationToken)
    {
        logger.LogInformation("StaticHandle : " + say.Text);
    }

    [ActionHandler]
    public async Task AsyncHandle(Say say, ILogger<ActionHandler> logger, CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);
        logger.LogInformation("AsyncHandled : " + say.Text);
    }
}
```
Method parameters follow the following rules
- The first parameter must be your `action` `message` `commond` `event`
- `CancellationToken` is passed from the caller
- Other parameters come from dependency injection

First, the ActionDispatcher will check the IServiceProvider to see if the ActionHandler has been added to the container. If it has, it will be obtained through dependency injection. Otherwise, a new ActionHandler will be created.

```c#
public class InjectServiceHandler(ILogger<InjectServiceHandler> logger)
{
    [ActionHandler]
    public void Handle(Say say)
    {
        logger.LogInformation("InjectServiceHandled : " + say.Text);
    }
}
```
```c#
services.AddSingleton<InjectServiceHandler>();
```

## Dispatcher
You can dispatch action with `IActionDispatcher`
```c#
var dispatcher = services.GetRequiredService<IActionDispatcher>();

dispatcher.Dispatch(new Say { Say = "Hello World" });
```
Asynchronous invocation is supported.
```c#
await dispatcher.DispatchAsync(new Say { Say = "Hello World" });
```
Log console output

```powershell
info: ActionHandler[0]
      StaticHandle : Hello World
info: ActionHandler[0]
      Handled : Hello World
info: InjectServiceHandler[0]
      InjectServiceHandled : Hello World
info: ActionHandler[0]
      AsyncHandled : Hello World
```

## Subscriber
You can subscribe action with `IActionSubscriber`, supprots `ActionDispatchStatus`:
- ActionDispatchStatus.WaitingToDispatch
- ActionDispatchStatus.Canceld
- ActionDispatchStatus.Successed
- ActionDispatchStatus.Faulted


```c#
var subscriber = services.GetRequiredService<IActionSubscriber>();

subscriber.Subscribe<Say>(action =>
{
    Console.WriteLine("Subscriber: Say action dispatched.");
});

subscriber.Subscribe<Say>(ActionDispatchStatus.WaitingToDispatch, action =>
{
    Console.WriteLine("Subscriber: Say action dispatching.");
});

subscriber.Subscribe<Say>(ActionDispatchStatus.Faulted, (action, exception) =>
{
    Console.WriteLine("Subscriber: Say action dispatch faulted.");
});

//Subscribe all types action
subscriber.Subscribe<object>(action =>
{
    Console.WriteLine($"Subscriber: {action.GetType()} action dispatched");
});

```

