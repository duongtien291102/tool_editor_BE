using AiVideoStudio.Application.Interfaces.OperationsAdmin;
using AiVideoStudio.Domain.Entities.OperationsAdmin;
using AiVideoStudio.Domain.Interfaces.OperationsAdmin;
using Microsoft.Extensions.Logging;

namespace AiVideoStudio.Application.Features.OperationsAdmin.Services;

public sealed class PlatformAdministrationService : IPlatformAdministrationService
{
    private readonly IPlatformAdministrationRepository _repository;
    private readonly IAuditService _auditService;
    private readonly ILogger<PlatformAdministrationService> _logger;

    public PlatformAdministrationService(
        IPlatformAdministrationRepository repository,
        IAuditService auditService,
        ILogger<PlatformAdministrationService> logger)
    {
        _repository = repository;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<PlatformConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var config = await _repository.GetConfigurationAsync(cancellationToken);
        if (config == null)
        {
            config = PlatformConfiguration.CreateDefault();
            await _repository.SaveConfigurationAsync(config, cancellationToken);
        }
        return config;
    }

    public async Task UpdateConfigurationAsync(
        int retentionDays,
        int maxConcurrentJobs,
        int maxDailyExports,
        string schedulerCron,
        bool enforceSecurity,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationAsync(cancellationToken);
        var beforeState = new { config.RetentionDays, config.MaxConcurrentJobsPerTenant, config.MaxDailyExportsPerUser, config.SchedulerCronExpression, config.EnforceStrictSecurity };

        config.UpdateSettings(retentionDays, maxConcurrentJobs, maxDailyExports, schedulerCron, enforceSecurity, updatedBy);
        await _repository.SaveConfigurationAsync(config, cancellationToken);

        var afterState = new { config.RetentionDays, config.MaxConcurrentJobsPerTenant, config.MaxDailyExportsPerUser, config.SchedulerCronExpression, config.EnforceStrictSecurity };
        await _auditService.LogAsync(updatedBy, updatedBy, "UpdateConfiguration", "PlatformConfiguration", config.Id, beforeState, afterState, cancellationToken: cancellationToken);

        _logger.LogInformation("Platform configuration updated by {UpdatedBy}", updatedBy);
    }

    public async Task<PlatformLicense?> GetLicenseAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetLicenseAsync(tenantId, cancellationToken);
    }

    public async Task UpdateLicenseAsync(
        string tenantId,
        string licenseKey,
        string licenseType,
        int maxSeats,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetLicenseAsync(tenantId, cancellationToken);
        if (existing == null)
        {
            existing = PlatformLicense.Create(tenantId, licenseKey, licenseType, maxSeats, TimeSpan.FromDays(365), updatedBy);
        }
        else
        {
            existing.Renew(TimeSpan.FromDays(365), updatedBy);
        }

        await _repository.SaveLicenseAsync(existing, cancellationToken);
        await _auditService.LogAsync(updatedBy, updatedBy, "UpdateLicense", "PlatformLicense", tenantId, afterState: new { tenantId, licenseType, maxSeats }, cancellationToken: cancellationToken);
        _logger.LogInformation("Platform license updated for tenant {TenantId}", tenantId);
    }

    public async Task<IReadOnlyList<MaintenanceWindow>> GetMaintenanceWindowsAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetMaintenanceWindowsAsync(cancellationToken);
    }

    public async Task<MaintenanceWindow> ScheduleMaintenanceAsync(
        string title,
        string description,
        DateTimeOffset start,
        DateTimeOffset end,
        bool readOnly,
        bool restart,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        var window = MaintenanceWindow.Create(title, description, start, end, readOnly, restart, createdBy);
        await _repository.SaveMaintenanceWindowAsync(window, cancellationToken);

        await _auditService.LogAsync(createdBy, createdBy, "ScheduleMaintenance", "MaintenanceWindow", window.Id, afterState: new { title, start, end }, cancellationToken: cancellationToken);
        _logger.LogWarning("Maintenance window scheduled: {Title} from {Start} to {End}", title, start, end);
        return window;
    }

    public async Task<bool> ReplayJobAsync(string jobType, string jobId, string replayedBy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId)) return false;

        await _auditService.LogAsync(replayedBy, replayedBy, "ReplayJob", jobType, jobId, afterState: new { jobType, jobId }, cancellationToken: cancellationToken);
        _logger.LogInformation("Job replay requested for [{JobType}:{JobId}] by {ReplayedBy}", jobType, jobId, replayedBy);
        return true;
    }
}
