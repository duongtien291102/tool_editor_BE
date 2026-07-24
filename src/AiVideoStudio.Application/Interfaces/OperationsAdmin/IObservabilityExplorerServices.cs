namespace AiVideoStudio.Application.Interfaces.OperationsAdmin;

public record LogExplorerEntry(string Timestamp, string Level, string Message, string CorrelationId, string SourceContext);
public record MetricExplorerEntry(string MetricName, double Value, string Unit, DateTimeOffset Timestamp);
public record TraceExplorerEntry(string TraceId, string SpanId, string OperationName, double DurationMs, string Status);

public interface ILogExplorerService
{
    Task<IReadOnlyList<LogExplorerEntry>> SearchLogsAsync(string? query = null, string? correlationId = null, int limit = 50, CancellationToken cancellationToken = default);
}

public interface IMetricsExplorerService
{
    Task<IReadOnlyList<MetricExplorerEntry>> QueryMetricsAsync(string? metricName = null, CancellationToken cancellationToken = default);
}

public interface ITraceExplorerService
{
    Task<IReadOnlyList<TraceExplorerEntry>> SearchTracesAsync(string? traceId = null, CancellationToken cancellationToken = default);
}
