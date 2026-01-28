namespace SourceGeneration.ActionDispatcher;

public enum ActionEventLevel
{
    Information,
    Warning,
    Success,
    Error,
}

public class ActionEventMessage(ActionEventLevel level, string? title, string? message, Exception? exception = null)
{
    public ActionEventLevel Level { get; } = level;
    public string? Title { get; } = title;
    public string? Message { get; } = message;
    public Exception? Exception { get; } = exception;
}

public static class ActionNotifierExtentions
{
    public static void Notify(this IActionNotifier notifier, ActionEventLevel type, string title, string? message = null, Exception? exception = null)
    {
        notifier.Notify(new ActionEventMessage(type, title, message, exception));
    }

    public static void NotifyInformation(this IActionNotifier notifier, string title, string? message = null) => notifier.Notify(ActionEventLevel.Information, title, message);
    public static void NotifySuccess(this IActionNotifier notifier, string title, string? message = null) => notifier.Notify(ActionEventLevel.Success, title, message);

    public static void NotifyWarning(this IActionNotifier notifier, string title, Exception exception) => notifier.Notify(ActionEventLevel.Warning, title, null, exception);
    public static void NotifyWarning(this IActionNotifier notifier, string title, string? message = null, Exception? exception = null) => notifier.Notify(ActionEventLevel.Warning, title, message, exception);

    public static void NotifyError(this IActionNotifier notifier, string title, Exception exception) => notifier.Notify(ActionEventLevel.Error, title, null, exception);
    public static void NotifyError(this IActionNotifier notifier, string title, string? message = null, Exception? exception = null) => notifier.Notify(ActionEventLevel.Error, title, message, exception);
}
