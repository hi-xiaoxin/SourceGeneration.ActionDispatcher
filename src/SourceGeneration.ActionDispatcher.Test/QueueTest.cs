using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SourceGeneration.ActionDispatcher.Test;

[TestClass]
public sealed class QueueTest
{
    public static readonly IActionDispatcher Dispatcher;
    public static readonly IServiceProvider Services;
    private static readonly TaskCompletionSource StartCompletion = new();
    private const int Tolerance = 50;

    static QueueTest()
    {
        Services = new ServiceCollection()
            .AddActionDispatcher()
            .AddActionQueue<TestAction>(options =>
            {
                options.IdSelector = x => x.Id;
            })
            .AddActionQueue<TimeAction>(options =>
            {
                options.IdSelector = x => x.Id;
            })
            .AddActionQueue<KeylessAction>()
            .AddActionQueue<StringKeyAction>()
            .AddSingleton<ActionHandle>()
            .BuildServiceProvider();

        Dispatcher = Services.GetRequiredService<IActionDispatcher>();

        var hosteds = Services.GetRequiredService<IEnumerable<IHostedService>>();

        Task.Run(async () =>
        {
            foreach (var hosted in hosteds)
                await hosted.StartAsync(CancellationToken.None).ConfigureAwait(false);

            StartCompletion.SetResult();
        });
    }

    [TestMethod]
    public async Task Enqueue()
    {
        await StartCompletion.Task;

        var action1 = new TestAction
        {
            Value = 10,
            Delay = 60,
        };
        var action2 = new TestAction
        {
            Value = 20,
            Delay = 30,
        };

        await Dispatcher.EnqueueAsync(action1).ConfigureAwait(false);
        await Dispatcher.EnqueueAsync(action2).ConfigureAwait(false);

        await Task.Delay(120, TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual(11, action1.Value);
        Assert.AreEqual(21, action2.Value);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(10)]
    [DataRow(100)]
    [DataRow(200)]
    [DataRow(500)]
    [DataRow(1000)]
    public async Task Schedule_Keyless(int intervalMs)
    {
        await StartCompletion.Task;
        var action1 = new KeylessAction();
        await Dispatcher.ScheduleAsync(action1, DateTimeOffset.UtcNow.AddMilliseconds(intervalMs)).ConfigureAwait(false);
        await Task.Delay(intervalMs + Tolerance, TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual(1, action1.Value);
    }

    [TestMethod]
    public async Task Cancel()
    {
        await StartCompletion.Task;
        var action1 = new TestAction { Value = 1 };
        await Dispatcher.ScheduleAsync(action1, DateTimeOffset.UtcNow.AddMilliseconds(100)).ConfigureAwait(false);
        await Task.Delay(50, TestContext.CancellationToken).ConfigureAwait(false);
        Dispatcher.Cancel<TestAction>(action1.Id);
        await Task.Delay(Tolerance, TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual(1, action1.Value);
    }

    [TestMethod]
    public async Task Cancel_Keyless()
    {
        await StartCompletion.Task;
        var action1 = new KeylessAction();

        await Dispatcher.ScheduleAsync(action1, DateTimeOffset.UtcNow.AddMilliseconds(100)).ConfigureAwait(false);
        Dispatcher.Cancel<KeylessAction>(action1);

        await Task.Delay(100 + Tolerance, TestContext.CancellationToken).ConfigureAwait(false);

        Assert.AreEqual(0, action1.Value);
    }

    [TestMethod]
    public async Task Cancel_Keyless_EqualsOverride()
    {
        await StartCompletion.Task;
        var action1 = new StringKeyAction { Key = "a" };

        await Dispatcher.ScheduleAsync(action1, DateTimeOffset.UtcNow.AddMilliseconds(100)).ConfigureAwait(false);

        Dispatcher.Cancel<StringKeyAction>(new StringKeyAction { Key = "a" });

        await Task.Delay(200, TestContext.CancellationToken).ConfigureAwait(false);

        Assert.AreEqual(0, action1.Value);
    }

    [TestMethod]
    public async Task Cancel_Id()
    {
        await StartCompletion.Task;

        var action1 = new TestAction { Value = 1, };
        var action2 = new TestAction { Value = 1, };
        var action3 = new TestAction { Value = 1, };
        var action4 = new TestAction { Value = 1, };

        await Dispatcher.ScheduleAsync([action1, action2, action3, action4], DateTimeOffset.UtcNow.AddMilliseconds(200)).ConfigureAwait(false);

        await Task.Delay(100, TestContext.CancellationToken).ConfigureAwait(false);

        Dispatcher.Cancel<TestAction>(action2.Id);
        Dispatcher.Cancel<TestAction>(action4.Id);

        await Task.Delay(100 + Tolerance, TestContext.CancellationToken).ConfigureAwait(false);

        Assert.AreEqual(2, action1.Value);
        Assert.AreEqual(1, action2.Value);
        Assert.AreEqual(2, action3.Value);
        Assert.AreEqual(1, action4.Value);
    }

    [TestMethod]
    [DoNotParallelize]
    [DataRow(10)]
    [DataRow(100)]
    [DataRow(1000)]
    [DataRow(2000)]
    [DataRow(3000)]
    public async Task Schedule(int intervalMs)
    {
        await StartCompletion.Task;

        var action1 = new TimeAction();
        await Dispatcher.ScheduleAsync(action1, DateTimeOffset.UtcNow.AddMilliseconds(intervalMs)).ConfigureAwait(false);
        await Task.Delay(intervalMs + 100, TestContext.CancellationToken).ConfigureAwait(false);

        //30秒误差
        Assert.IsGreaterThanOrEqualTo(0, action1.IntervalMs);
        Assert.IsLessThan(Tolerance, action1.IntervalMs);
    }

    public TestContext TestContext { get; set; }
}

public class TestAction
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public int Value { get; set; }
    public int Delay { get; set; }
}

public class TimeAction
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public long ExecutedMs { get; set; }
    public long IntervalMs { get; set; }
}

public class StringKeyAction
{
    public string? Key { get; set; }
    public override bool Equals(object? obj)
    {
        return obj is StringKeyAction other && other.Key == Key;
    }

    public int Value { get; set; }
    public override int GetHashCode() => HashCode.Combine(Key);

}
public class KeylessAction
{
    public int Value { get; set; }

}

public class ActionHandle
{
    [ActionHandler]
    public async Task HandleAsync(TestAction action)
    {
        if (action.Delay > 0)
            await Task.Delay(action.Delay).ConfigureAwait(false);
        action.Value++;
    }

    [ActionHandler]
    public static void HandleAsync(KeylessAction action)
    {
        action.Value++;
    }

    [ActionHandler]
    public static void Handle(ActionTaskQueueContext<TimeAction> context)
    {
        context.Action.ExecutedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        context.Action.IntervalMs = context.Action.ExecutedMs - context.ScheduledAtMs;
    }
}