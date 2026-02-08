// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SourceGeneration.ActionDispatcher;
using SourceGeneration.ActionDispatcher.Test;

var builder = Host.CreateApplicationBuilder();

builder.Services.AddLogging();
builder.Services.AddActionDispatcher();
builder.Services.AddActionTaskQueue<Action1>(options => options.MaxConcurrency = 30);

builder.Services.AddSingleton<IHandler, Handler>();
var app = builder.Build();

await app.StartAsync();

var dispatcher = app.Services.GetRequiredService<IActionDispatcher>();
var subscriber = app.Services.GetRequiredService<IActionSubscriber>();

int count = 0;
int batch = 1;
int loop = 1;

subscriber.Subscribe<Action1>(DispatchStatus.WaitingForActivation, action => Console.WriteLine("WaitingForActivation"));
subscriber.Subscribe<Action1>(DispatchStatus.WaitingToRun, action => Console.WriteLine("WaitingToRun"));
subscriber.Subscribe<Action1>(DispatchStatus.Running, action => Console.WriteLine("Running"));
subscriber.Subscribe<Action1>(DispatchStatus.Succeeded, action => Console.WriteLine("Succeeded"));

subscriber.Subscribe<Action1>(action =>
{
    Interlocked.Increment(ref count);
    if (count == batch * loop)
    {
        Console.WriteLine($"Completion");
    }
});


var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
for (int i = 0; i < loop; i++)
{
    var items = Enumerable.Range(0, batch).Select(x => new DispatchItem<Action1>
    {
        Data = new Action1 { Result = x * (i + 1) },
    }).ToList();

    _ = dispatcher.ScheduleAsync<Action1>(items, now + Random.Shared.Next(100, 2000));
}
//dispatcher.Execute(action);

//Console.WriteLine(action.Result);
Console.ReadLine();

class Action1
{
    public int Result;
};