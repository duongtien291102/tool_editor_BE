using AiVideoStudio.Shared.Responses;
using AiVideoStudio.Application.Features.OperationsAdmin.Commands;
using AiVideoStudio.Application.Interfaces.OperationsAdmin;
using AiVideoStudio.Domain.Entities.OperationsAdmin;
using MediatR;

namespace AiVideoStudio.Application.Features.OperationsAdmin.Handlers;

public sealed class AdministrationHandlers :
    IRequestHandler<GetPlatformConfigurationQuery, Result<PlatformConfiguration>>,
    IRequestHandler<UpdatePlatformConfigurationCommand, Result<Unit>>,
    IRequestHandler<GetFeatureFlagsQuery, Result<IReadOnlyDictionary<string, bool>>>,
    IRequestHandler<SetFeatureFlagCommand, Result<Unit>>,
    IRequestHandler<GetAuditLogsQuery, Result<IReadOnlyList<PlatformAuditLogEntry>>>,
    IRequestHandler<CreateIncidentCommand, Result<PlatformIncident>>,
    IRequestHandler<ResolveIncidentCommand, Result<PlatformIncident>>,
    IRequestHandler<GetIncidentsQuery, Result<IReadOnlyList<PlatformIncident>>>,
    IRequestHandler<CreateBackupCommand, Result<BackupSnapshot>>,
    IRequestHandler<RestoreBackupCommand, Result<bool>>,
    IRequestHandler<ScheduleMaintenanceCommand, Result<MaintenanceWindow>>,
    IRequestHandler<ReplayJobCommand, Result<bool>>,
    IRequestHandler<GetOperationsDashboardQuery, Result<OperationsDashboardSnapshot>>
{
    private readonly IPlatformAdministrationService _adminService;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IAuditService _auditService;
    private readonly IIncidentManager _incidentManager;
    private readonly IBackupService _backupService;
    private readonly IPlatformHealthService _healthService;

    public AdministrationHandlers(
        IPlatformAdministrationService adminService,
        IFeatureFlagService featureFlagService,
        IAuditService auditService,
        IIncidentManager incidentManager,
        IBackupService backupService,
        IPlatformHealthService healthService)
    {
        _adminService = adminService;
        _featureFlagService = featureFlagService;
        _auditService = auditService;
        _incidentManager = incidentManager;
        _backupService = backupService;
        _healthService = healthService;
    }

    public async Task<Result<PlatformConfiguration>> Handle(GetPlatformConfigurationQuery request, CancellationToken cancellationToken)
    {
        var config = await _adminService.GetConfigurationAsync(cancellationToken);
        return Result<PlatformConfiguration>.Success(config);
    }

    public async Task<Result<Unit>> Handle(UpdatePlatformConfigurationCommand request, CancellationToken cancellationToken)
    {
        await _adminService.UpdateConfigurationAsync(
            request.RetentionDays,
            request.MaxConcurrentJobsPerTenant,
            request.MaxDailyExportsPerUser,
            request.SchedulerCronExpression,
            request.EnforceStrictSecurity,
            request.UpdatedBy,
            cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }

    public async Task<Result<IReadOnlyDictionary<string, bool>>> Handle(GetFeatureFlagsQuery request, CancellationToken cancellationToken)
    {
        var flags = await _featureFlagService.GetAllFlagsAsync(cancellationToken);
        return Result<IReadOnlyDictionary<string, bool>>.Success(flags);
    }

    public async Task<Result<Unit>> Handle(SetFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        await _featureFlagService.SetFlagAsync(request.FlagName, request.Enabled, request.UpdatedBy, cancellationToken);
        return Result<Unit>.Success(Unit.Value);
    }

    public async Task<Result<IReadOnlyList<PlatformAuditLogEntry>>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var logs = await _auditService.QueryAuditLogsAsync(request.Skip, request.Take, cancellationToken);
        return Result<IReadOnlyList<PlatformAuditLogEntry>>.Success(logs);
    }

    public async Task<Result<PlatformIncident>> Handle(CreateIncidentCommand request, CancellationToken cancellationToken)
    {
        var incident = await _incidentManager.CreateIncidentAsync(request.Title, request.Description, request.Severity, request.CreatedBy, cancellationToken);
        return Result<PlatformIncident>.Success(incident);
    }

    public async Task<Result<PlatformIncident>> Handle(ResolveIncidentCommand request, CancellationToken cancellationToken)
    {
        var incident = await _incidentManager.ResolveIncidentAsync(request.IncidentId, request.RootCause, request.Resolution, request.UpdatedBy, cancellationToken);
        if (incident == null) return Result<PlatformIncident>.Failure(new AiVideoStudio.Shared.DomainErrors.Error("Incident.NotFound", "Incident not found."));
        return Result<PlatformIncident>.Success(incident);
    }

    public async Task<Result<IReadOnlyList<PlatformIncident>>> Handle(GetIncidentsQuery request, CancellationToken cancellationToken)
    {
        var incidents = await _incidentManager.GetIncidentsAsync(request.Status, cancellationToken);
        return Result<IReadOnlyList<PlatformIncident>>.Success(incidents);
    }

    public async Task<Result<BackupSnapshot>> Handle(CreateBackupCommand request, CancellationToken cancellationToken)
    {
        var snapshot = await _backupService.CreateBackupAsync(request.BackupType, request.CreatedBy, cancellationToken);
        return Result<BackupSnapshot>.Success(snapshot);
    }

    public async Task<Result<bool>> Handle(RestoreBackupCommand request, CancellationToken cancellationToken)
    {
        var success = await _backupService.RestoreBackupAsync(request.SnapshotId, request.RequestedBy, cancellationToken);
        return Result<bool>.Success(success);
    }

    public async Task<Result<MaintenanceWindow>> Handle(ScheduleMaintenanceCommand request, CancellationToken cancellationToken)
    {
        var window = await _adminService.ScheduleMaintenanceAsync(
            request.Title,
            request.Description,
            request.ScheduledStart,
            request.ScheduledEnd,
            request.IsReadOnlyMode,
            request.SystemRestartRequired,
            request.CreatedBy,
            cancellationToken);

        return Result<MaintenanceWindow>.Success(window);
    }

    public async Task<Result<bool>> Handle(ReplayJobCommand request, CancellationToken cancellationToken)
    {
        var success = await _adminService.ReplayJobAsync(request.JobType, request.JobId, request.ReplayedBy, cancellationToken);
        return Result<bool>.Success(success);
    }

    public async Task<Result<OperationsDashboardSnapshot>> Handle(GetOperationsDashboardQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await _healthService.GetOperationsDashboardSnapshotAsync(cancellationToken);
        return Result<OperationsDashboardSnapshot>.Success(snapshot);
    }
}
