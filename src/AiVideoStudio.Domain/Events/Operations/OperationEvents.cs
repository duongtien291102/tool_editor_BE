using AiVideoStudio.Domain.Base;
namespace AiVideoStudio.Domain.Events.Operations;
public abstract record OperationEvent:IDomainEvent{public DateTimeOffset OccurredOn{get;}=DateTimeOffset.UtcNow;}
public sealed record AuditLogCreatedEvent(string AuditLogId):OperationEvent;
public sealed record NotificationCreatedEvent(string NotificationId,string UserId):OperationEvent;
public sealed record QuotaExceededEvent(string UserId,string Quota):OperationEvent;
public sealed record MaintenanceStartedEvent(string MaintenanceId):OperationEvent;
public sealed record MaintenanceCompletedEvent(string MaintenanceId):OperationEvent;
