using AiVideoStudio.Domain.Entities.OperationsAdmin;
using AiVideoStudio.Domain.Interfaces.OperationsAdmin;
using MongoDB.Driver;

namespace AiVideoStudio.Infrastructure.Mongo.Repositories;

public sealed class PlatformAdministrationRepository : IPlatformAdministrationRepository
{
    private readonly MongoDbContext _context;

    public PlatformAdministrationRepository(MongoDbContext context)
    {
        _context = context;
    }

    private IMongoCollection<PlatformConfiguration> Configurations => _context.Database.GetCollection<PlatformConfiguration>("platform_configurations");
    private IMongoCollection<PlatformLicense> Licenses => _context.Database.GetCollection<PlatformLicense>("platform_licenses");
    private IMongoCollection<PlatformAuditLogEntry> AuditLogs => _context.Database.GetCollection<PlatformAuditLogEntry>("platform_audit_logs");
    private IMongoCollection<PlatformIncident> Incidents => _context.Database.GetCollection<PlatformIncident>("platform_incidents");
    private IMongoCollection<MaintenanceWindow> MaintenanceWindows => _context.Database.GetCollection<MaintenanceWindow>("maintenance_windows");
    private IMongoCollection<BackupSnapshot> Backups => _context.Database.GetCollection<BackupSnapshot>("backup_snapshots");
    private IMongoCollection<PlatformAlert> Alerts => _context.Database.GetCollection<PlatformAlert>("platform_alerts");

    public async Task<PlatformConfiguration?> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await Configurations.Find(FilterDefinition<PlatformConfiguration>.Empty).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveConfigurationAsync(PlatformConfiguration config, CancellationToken cancellationToken = default)
    {
        await Configurations.ReplaceOneAsync(
            c => c.Id == config.Id,
            config,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<PlatformLicense?> GetLicenseAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return await Licenses.Find(l => l.TenantId == tenantId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveLicenseAsync(PlatformLicense license, CancellationToken cancellationToken = default)
    {
        await Licenses.ReplaceOneAsync(
            l => l.Id == license.Id,
            license,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task AddAuditLogAsync(PlatformAuditLogEntry auditLog, CancellationToken cancellationToken = default)
    {
        await AuditLogs.InsertOneAsync(auditLog, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformAuditLogEntry>> GetAuditLogsAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await AuditLogs.Find(FilterDefinition<PlatformAuditLogEntry>.Empty)
            .SortByDescending(a => a.Timestamp)
            .Skip(skip)
            .Limit(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<PlatformIncident?> GetIncidentByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Incidents.Find(i => i.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformIncident>> GetIncidentsAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var filter = string.IsNullOrWhiteSpace(status)
            ? FilterDefinition<PlatformIncident>.Empty
            : Builders<PlatformIncident>.Filter.Eq(i => i.Status, status);

        return await Incidents.Find(filter).SortByDescending(i => i.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task SaveIncidentAsync(PlatformIncident incident, CancellationToken cancellationToken = default)
    {
        await Incidents.ReplaceOneAsync(
            i => i.Id == incident.Id,
            incident,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceWindow>> GetMaintenanceWindowsAsync(CancellationToken cancellationToken = default)
    {
        return await MaintenanceWindows.Find(FilterDefinition<MaintenanceWindow>.Empty).SortByDescending(m => m.ScheduledStart).ToListAsync(cancellationToken);
    }

    public async Task SaveMaintenanceWindowAsync(MaintenanceWindow window, CancellationToken cancellationToken = default)
    {
        await MaintenanceWindows.ReplaceOneAsync(
            m => m.Id == window.Id,
            window,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<IReadOnlyList<BackupSnapshot>> GetBackupsAsync(CancellationToken cancellationToken = default)
    {
        return await Backups.Find(FilterDefinition<BackupSnapshot>.Empty).SortByDescending(b => b.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task SaveBackupSnapshotAsync(BackupSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await Backups.ReplaceOneAsync(
            b => b.Id == snapshot.Id,
            snapshot,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformAlert>> GetAlertsAsync(CancellationToken cancellationToken = default)
    {
        return await Alerts.Find(FilterDefinition<PlatformAlert>.Empty).ToListAsync(cancellationToken);
    }

    public async Task SaveAlertAsync(PlatformAlert alert, CancellationToken cancellationToken = default)
    {
        await Alerts.ReplaceOneAsync(
            a => a.Id == alert.Id,
            alert,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }
}
