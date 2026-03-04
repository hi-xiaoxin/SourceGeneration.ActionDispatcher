namespace SourceGeneration.ActionDispatcher;

[Flags]
public enum DispatchStatus
{
    Created = 0,
    WaitingForActivation = 1,
    WaitingToRun = 2,
    Running = 4,
    Succeeded = 8,
    Canceled = 16,
    Faulted = 32,
    Completed = Succeeded | Canceled | Faulted,
}