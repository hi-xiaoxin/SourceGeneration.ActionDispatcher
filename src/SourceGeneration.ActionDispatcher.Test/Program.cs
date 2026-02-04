// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SourceGeneration.ActionDispatcher;
using SourceGeneration.ActionDispatcher.Test;

var builder = Host.CreateApplicationBuilder();

builder.Services.AddLogging();
builder.Services.AddActionDispatcher();
builder.Services.AddBackgroundTaskHandler<Action1>();

builder.Services.AddSingleton<IHandler, Handler>();
var app = builder.Build();

await app.StartAsync();

var dispatcher = app.Services.GetRequiredService<IActionDispatcher>();
var subscriber = app.Services.GetRequiredService<IActionSubscriber>();

subscriber.Subscribe<Action1>(action => Console.WriteLine($"subed {action.Result}"));

var action = new Action1();
await dispatcher.ScheduleAsync(action, DateTimeOffset.UtcNow.AddSeconds(10));
//dispatcher.Execute(action);

//Console.WriteLine(action.Result);
Console.ReadLine();

class Action1
{
    public int Result;
};