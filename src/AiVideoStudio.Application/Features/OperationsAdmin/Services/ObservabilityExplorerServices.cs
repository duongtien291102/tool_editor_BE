using AiVideoStudio.Application.Interfaces.OperationsAdmin;
using Microsoft.Extensions.Logging;

namespace AiVideoStudio.Application.Features.OperationsAdmin.Services;

public sealed class LogExplorerService : ILogExplorerService
{
    private readonly ILogger<LogExplorerService> _logger;

    public LogExplorerService(ILogger<LogExplorerService> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<LogExplorerEntry>> SearchLogsAsync(string? query = null, string? correlationId = null, int limit = 50, CancellationToken cancellationToken = default)
    {
        var logs = new List<LogExplorerEntry>
        {
            new(DateTimeOffset.UtcNow.AddMinutes(-5).ToString("o"), "Information", "Workflow execution completed successfully.", correlationId ?? Guid.NewGuid().ToString("N"), "WorkflowEngine"),
            new(DateTimeOffset.UtcNow.AddMinutes(-3).ToString("o"), "Warning", "Provider capacity near threshold (85%).", correlationId ?? Guid.NewGuid().ToString("N"), "ProviderEngine"),
            new(DateTimeOffset.UtcNow.AddMinutes(-1).ToString("o"), "Information", "Export package generated and uploaded.", correlationId ?? Guid.NewGuid().ToString("N"), "ExportEngine")
        };

        if (!string.IsNullOrWhiteSpace(query))
        {
            logs = logs.Where(l => l.Message.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return Task.FromResult<IReadOnlyList<LogExplorerEntry>>(logs.Take(limit).ToList());
    }
}

public sealed class MetricsExplorerService : IMetricsExplorerService
{
    public Task<IReadOnlyList<MetricExplorerEntry>> QueryMetricsAsync(string? metricName = null, CancellationToken cancellationToken = default)
    {
        var metrics = new List<MetricExplorerEntry>
        {
            new("platform_uptime_seconds", 86400, "Seconds", DateTimeOffset.UtcNow),
            new("active_users", 42, "Count", DateTimeOffset.UtcNow),
            new("active_workflows", 15, "Count", DateTimeOffset.UtcNow),
            new("active_render_jobs", 8, "Count", DateTimeOffset.UtcNow),
            new("queue_depth", 3, "Count", DateTimeOffset.UtcNow),
            new("memory_usage_mb", 512, "Megabytes", DateTimeOffset.UtcNow),
            new("cpu_usage_percent", 14.5, "Percent", DateTimeOffset.UtcNow)
        };

        if (!string.IsNullOrWhiteSpace(metricName))
        {
            metrics = metrics.Where(m => m.MetricName.Equals(metricName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return Task.FromResult<IReadOnlyList<MetricExplorerEntry>>(metrics);
    }
}

public sealed class TraceExplorerService : ITraceExplorerService
{
    public Task<IReadOnlyList<TraceExplorerEntry>> SearchTracesAsync(string? traceId = null, CancellationToken cancellationToken = default)
    {
        var traces = new List<TraceExplorerEntry>
        {
            new(traceId ?? Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), "POST /api/v1/workflows/execute", 142.5, "Success"),
            new(traceId ?? Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), "RenderEngine.ProcessJob", 850.2, "Success"),
            new(traceId ?? Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), "ExportEngine.CompileGraph", 310.0, "Success")
        };

        return Task.FromResult<IReadOnlyList<TraceExplorerEntry>>(traces);
    }
}
