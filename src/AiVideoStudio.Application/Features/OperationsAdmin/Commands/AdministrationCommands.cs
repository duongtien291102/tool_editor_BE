using AiVideoStudio.Shared.Responses;
using AiVideoStudio.Application.Interfaces.OperationsAdmin;
using AiVideoStudio.Domain.Entities.OperationsAdmin;
using MediatR;

namespace AiVideoStudio.Application.Features.OperationsAdmin.Commands;

public record GetPlatformConfigurationQuery : IRequest<Result<PlatformConfiguration>>;

public record UpdatePlatformConfigurationCommand(
    int RetentionDays,
    int MaxConcurrentJobsPerTenant,
    int MaxDailyExportsPerUser,
    string SchedulerCronExpression,
    bool EnforceStrictSecurity,
    string UpdatedBy) : IRequest<Result<Unit>>;

public record GetFeatureFlagsQuery : IRequest<Result<IReadOnlyDictionary<string, bool>>>;

public record SetFeatureFlagCommand(
    string FlagName,
    bool Enabled,
    string UpdatedBy) : IRequest<Result<Unit>>;

public record GetAuditLogsQuery(int Skip = 0, int Take = 50) : IRequest<Result<IReadOnlyList<PlatformAuditLogEntry>>>;

public record CreateIncidentCommand(
    string Title,
    string Description,
    string Severity,
    string CreatedBy) : IRequest<Result<PlatformIncident>>;

public record ResolveIncidentCommand(
    string IncidentId,
    string RootCause,
    string Resolution,
    string UpdatedBy) : IRequest<Result<PlatformIncident>>;

public record GetIncidentsQuery(string? Status = null) : IRequest<Result<IReadOnlyList<PlatformIncident>>>;

public record CreateBackupCommand(
    string BackupType,
    string CreatedBy) : IRequest<Result<BackupSnapshot>>;

public record RestoreBackupCommand(
    string SnapshotId,
    string RequestedBy) : IRequest<Result<bool>>;

public record ScheduleMaintenanceCommand(
    string Title,
    string Description,
    DateTimeOffset ScheduledStart,
    DateTimeOffset ScheduledEnd,
    bool IsReadOnlyMode,
    bool SystemRestartRequired,
    string CreatedBy) : IRequest<Result<MaintenanceWindow>>;

public record ReplayJobCommand(
    string JobType,
    string JobId,
    string ReplayedBy) : IRequest<Result<bool>>;

public record GetOperationsDashboardQuery : IRequest<Result<OperationsDashboardSnapshot>>;
