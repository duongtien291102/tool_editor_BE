using System.Diagnostics.Metrics;

namespace AiVideoStudio.Infrastructure.Orchestration;

public sealed class OrchestrationMetrics
{
    public const string MeterName = "AiVideoStudio.OrchestrationEngine";

    private readonly Counter<long> _workflowsStarted;
    private readonly Counter<long> _workflowsCompleted;
    private readonly Counter<long> _workflowsFailed;
    private readonly Histogram<double> _workflowDuration;
    private readonly Counter<long> _parallelStepsCount;
    private readonly Counter<long> _batchCount;
    private readonly Counter<long> _retryCount;

    public OrchestrationMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _workflowsStarted = meter.CreateCounter<long>("workflow_started_total", "Count of started workflows");
        _workflowsCompleted = meter.CreateCounter<long>("workflow_completed_total", "Count of completed workflows");
        _workflowsFailed = meter.CreateCounter<long>("workflow_failed_total", "Count of failed workflows");
        _workflowDuration = meter.CreateHistogram<double>("workflow_duration_seconds", "Seconds", "Duration of workflow execution");
        _parallelStepsCount = meter.CreateCounter<long>("workflow_parallel_steps_total", "Count of steps executed in parallel");
        _batchCount = meter.CreateCounter<long>("workflow_batch_total", "Count of shot batches processed");
        _retryCount = meter.CreateCounter<long>("workflow_retry_total", "Count of step retries attempted");
    }

    public void RecordStarted() => _workflowsStarted.Add(1);
    public void RecordCompleted() => _workflowsCompleted.Add(1);
    public void RecordFailed() => _workflowsFailed.Add(1);
    public void RecordDuration(double seconds) => _workflowDuration.Record(seconds);
    public void RecordParallelSteps(int count) => _parallelStepsCount.Add(count);
    public void RecordBatch() => _batchCount.Add(1);
    public void RecordRetry() => _retryCount.Add(1);
}
