using System.Diagnostics.CodeAnalysis;

namespace SourceGeneration.ActionDispatcher;

internal readonly struct TaskId(Guid id, long businessId) : IEquatable<TaskId>
{
    public readonly Guid Id = id;
    public readonly long BusinessId = businessId;

    public bool Equals(TaskId other)
    {
        return other.Id.Equals(Id) || (BusinessId > 0 && other.BusinessId == BusinessId);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is TaskId tid && Equals(tid);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, BusinessId);
    }
}
