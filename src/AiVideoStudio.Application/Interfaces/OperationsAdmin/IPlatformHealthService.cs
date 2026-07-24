namespace AiVideoStudio.Application.Interfaces.OperationsAdmin;

public record ComponentHealthStatus(string ComponentName, string Status, string Description, TimeSpan Latency);

public record OperationsDashboardSnapshot(
    int ActiveUsers,
    int ActiveWorkflows,
    int ActiveRenderJobs,
    int ActiveExports,
    int QueueDepth,
    int WorkerCount,
    double CpuUsagePercent,
    double MemoryUsageMb,
    int BrowserPoolUsage,
    int ProviderCapacityRemainingPercent,
    string ClusterStatus,
    IReadOnlyList<ComponentHealthStatus> SubsystemHealth);

public interface IPlatformHealthService
{
    Task<OperationsDashboardSnapshot> GetOperationsDashboardSnapshotAsync(CancellationToken cancellationToken = default);
}
