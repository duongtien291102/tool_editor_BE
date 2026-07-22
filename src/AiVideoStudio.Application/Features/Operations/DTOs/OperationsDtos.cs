using AiVideoStudio.Domain.Enums;
namespace AiVideoStudio.Application.Features.Operations.DTOs;
public record AuditLogDto(string Id,string UserId,string Action,string Result,string CorrelationId,string TraceId,DateTimeOffset Timestamp);
public record NotificationDto(string Id,string UserId,string Title,string Message,NotificationType Type,bool IsRead,DateTimeOffset CreatedAt);
public record QuotaDto(string UserId,QuotaType Type,long Limit,long Used,long Remaining,bool Exceeded);
public record UsageDto(string Id,string UserId,QuotaType Type,long Amount,string ResourceId,DateTimeOffset CreatedAt);
public record ConfigurationDto(string Key,string Value,bool IsSensitive,DateTimeOffset? UpdatedAt);
public record MaintenanceDto(string Id,string Name,MaintenanceStatus Status,int DeletedRecords,string? Error,DateTimeOffset? StartedAt,DateTimeOffset? CompletedAt);
