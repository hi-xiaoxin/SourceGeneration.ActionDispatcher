// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SourceGeneration.ActionDispatcher;
using SourceGeneration.ActionDispatcher.Test;


var builder = Host.CreateApplicationBuilder();

builder.Services.AddLogging();
builder.Services.AddActionDispatcher();
builder.Services.AddActionQueue<int, Action1>(options => options.MaxConcurrency = 30);

builder.Services.AddSingleton<IHandler, Handler>();
var app = builder.Build();

await app.StartAsync();

var dispatcher = app.Services.GetRequiredService<IActionDispatcher>();
var subscriber = app.Services.GetRequiredService<IActionSubscriber>();

subscriber.Subscribe<Action1>(DispatchStatus.WaitingForActivation, action => Console.WriteLine("WaitingForActivation"));
subscriber.Subscribe<Action1>(DispatchStatus.WaitingToRun, action => Console.WriteLine("WaitingToRun"));
subscriber.Subscribe<Action1>(DispatchStatus.Running, action => Console.WriteLine("Running"));
subscriber.Subscribe<Action1>(DispatchStatus.Succeeded, action => Console.WriteLine("Succeeded"));
subscriber.Subscribe<Action1>(DispatchStatus.Canceled, action => Console.WriteLine("Canceled"));

//for (int i = 0; i < 1; i++)
//{
//    int id = i;
//    _ = dispatcher.ScheduleAsync(id, new Action1 { Result = 1 }, scheduledAt: DateTimeOffset.UtcNow.AddSeconds(1));
//    _ = Task.Run(async () =>
//    {
//        await Task.Delay(Random.Shared.Next(100, 800));
//        dispatcher.Cancel<int, Action1>(id);
//    });
//}


Test1(dispatcher, subscriber);


//Console.WriteLine(action.Result);
Console.ReadLine();

static void Test1(IActionDispatcher dispatcher, IActionSubscriber subscriber)
{
    int count = 0;
    int batch = 10;
    int loop = 10;


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
        var items = Enumerable.Range(0, batch).Select(x => new DispatchItem<int, Action1>
        {
            Id = x,
            Data = new Action1 { Result = x * (i + 1) },
        }).ToList();

        _ = dispatcher.ScheduleAsync<int, Action1>(items, now + Random.Shared.Next(100, 5000));
    }
}

class Action1
{
    public int Result;
};