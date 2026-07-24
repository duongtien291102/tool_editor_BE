using AiVideoStudio.Domain.Entities.OperationsAdmin;

namespace AiVideoStudio.Domain.Interfaces.OperationsAdmin;

public interface IPlatformAdministrationRepository
{
    Task<PlatformConfiguration?> GetConfigurationAsync(CancellationToken cancellationToken = default);
    Task SaveConfigurationAsync(PlatformConfiguration config, CancellationToken cancellationToken = default);

    Task<PlatformLicense?> GetLicenseAsync(string tenantId, CancellationToken cancellationToken = default);
    Task SaveLicenseAsync(PlatformLicense license, CancellationToken cancellationToken = default);

    Task AddAuditLogAsync(PlatformAuditLogEntry auditLog, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlatformAuditLogEntry>> GetAuditLogsAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    Task<PlatformIncident?> GetIncidentByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlatformIncident>> GetIncidentsAsync(string? status = null, CancellationToken cancellationToken = default);
    Task SaveIncidentAsync(PlatformIncident incident, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceWindow>> GetMaintenanceWindowsAsync(CancellationToken cancellationToken = default);
    Task SaveMaintenanceWindowAsync(MaintenanceWindow window, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BackupSnapshot>> GetBackupsAsync(CancellationToken cancellationToken = default);
    Task SaveBackupSnapshotAsync(BackupSnapshot snapshot, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlatformAlert>> GetAlertsAsync(CancellationToken cancellationToken = default);
    Task SaveAlertAsync(PlatformAlert alert, CancellationToken cancellationToken = default);
}
