using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Events.Exports;

public record ExportStartedEvent(string ExportJobId, string OwnerId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public record ExportProgressChangedEvent(string ExportJobId, int Progress) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public record ExportCompletedEvent(string ExportJobId, string OutputPath) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public record ExportFailedEvent(string ExportJobId, string ErrorMessage) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public record ExportCancelledEvent(string ExportJobId, string CancelledBy) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
