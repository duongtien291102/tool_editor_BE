using AiVideoStudio.Domain.Entities.OperationsAdmin;

namespace AiVideoStudio.Application.Interfaces.OperationsAdmin;

public interface IPlatformAdministrationService
{
    Task<PlatformConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);
    Task UpdateConfigurationAsync(int retentionDays, int maxConcurrentJobs, int maxDailyExports, string schedulerCron, bool enforceSecurity, string updatedBy, CancellationToken cancellationToken = default);

    Task<PlatformLicense?> GetLicenseAsync(string tenantId, CancellationToken cancellationToken = default);
    Task UpdateLicenseAsync(string tenantId, string licenseKey, string licenseType, int maxSeats, string updatedBy, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceWindow>> GetMaintenanceWindowsAsync(CancellationToken cancellationToken = default);
    Task<MaintenanceWindow> ScheduleMaintenanceAsync(string title, string description, DateTimeOffset start, DateTimeOffset end, bool readOnly, bool restart, string createdBy, CancellationToken cancellationToken = default);

    Task<bool> ReplayJobAsync(string jobType, string jobId, string replayedBy, CancellationToken cancellationToken = default);
}
