using System.Diagnostics.Metrics;

namespace AiVideoStudio.Infrastructure.OperationsAdmin.Telemetry;

public sealed class PlatformTelemetry
{
    public static readonly string MeterName = "AiVideoStudio.OperationsAdmin";
    private static readonly Meter Meter = new(MeterName, "1.0.0");

    public Counter<long> UptimeSeconds { get; }
    public UpDownCounter<long> ActiveUsers { get; }
    public UpDownCounter<long> ActiveWorkflows { get; }
    public UpDownCounter<long> ActiveRenderJobs { get; }
    public UpDownCounter<long> ActiveExports { get; }
    public UpDownCounter<long> BrowserPoolUsage { get; }
    public UpDownCounter<long> ProviderCapacity { get; }
    public UpDownCounter<long> QueueDepth { get; }
    public UpDownCounter<long> StorageUsageBytes { get; }
    public Counter<long> BackupDurationMs { get; }
    public Counter<long> RestoreDurationMs { get; }
    public Counter<long> IncidentTotal { get; }
    public Counter<long> AlertsTotal { get; }

    public PlatformTelemetry()
    {
        UptimeSeconds = Meter.CreateCounter<long>("platform_uptime_seconds", "s", "Platform uptime in seconds");
        ActiveUsers = Meter.CreateUpDownCounter<long>("active_users", "{user}", "Active user count");
        ActiveWorkflows = Meter.CreateUpDownCounter<long>("active_workflows", "{workflow}", "Active workflow count");
        ActiveRenderJobs = Meter.CreateUpDownCounter<long>("active_render_jobs", "{job}", "Active render job count");
        ActiveExports = Meter.CreateUpDownCounter<long>("active_exports", "{export}", "Active export job count");
        BrowserPoolUsage = Meter.CreateUpDownCounter<long>("browser_pool_usage", "{instance}", "Browser pool active instances");
        ProviderCapacity = Meter.CreateUpDownCounter<long>("provider_capacity", "%", "Provider capacity remaining");
        QueueDepth = Meter.CreateUpDownCounter<long>("queue_depth", "{item}", "Queue depth across cluster");
        StorageUsageBytes = Meter.CreateUpDownCounter<long>("storage_usage", "By", "Storage usage in bytes");
        BackupDurationMs = Meter.CreateCounter<long>("backup_duration", "ms", "Backup duration in ms");
        RestoreDurationMs = Meter.CreateCounter<long>("restore_duration", "ms", "Restore duration in ms");
        IncidentTotal = Meter.CreateCounter<long>("incident_total", "{incident}", "Total incident count");
        AlertsTotal = Meter.CreateCounter<long>("alerts_total", "{alert}", "Total alert count");
    }
}
