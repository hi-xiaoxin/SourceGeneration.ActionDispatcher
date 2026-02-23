namespace SourceGeneration.ActionDispatcher;

public enum ActionEventLevel
{
    Information,
    Warning,
    Success,
    Faulted,
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
    public static void Notify(this IActionDispatcher notifier, ActionEventLevel type, string title, string? message = null, Exception? exception = null)
    {
        notifier.Notify(new ActionEventMessage(type, title, message, exception));
    }

    public static void NotifyInformation(this IActionDispatcher notifier, string title, string? message = null) => notifier.Notify(ActionEventLevel.Information, title, message);
    public static void NotifySuccess(this IActionDispatcher notifier, string title, string? message = null) => notifier.Notify(ActionEventLevel.Success, title, message);

    public static void NotifyWarning(this IActionDispatcher notifier, string title, Exception exception) => notifier.Notify(ActionEventLevel.Warning, title, exception.Message, exception);
    public static void NotifyWarning(this IActionDispatcher notifier, string title, string? message = null, Exception? exception = null) => notifier.Notify(ActionEventLevel.Warning, title, message, exception);

    public static void NotifyError(this IActionDispatcher notifier, string title, Exception exception) => notifier.Notify(ActionEventLevel.Faulted, title, exception.Message, exception);
    public static void NotifyError(this IActionDispatcher notifier, string title, string? message = null, Exception? exception = null) => notifier.Notify(ActionEventLevel.Faulted, title, message, exception);
}