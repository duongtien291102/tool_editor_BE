using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Domain.Entities.Orchestration;

public sealed class WorkflowResult
{
    public string WorkflowId { get; private set; } = string.Empty;
    public WorkflowState Status { get; private set; }
    public bool IsSuccess => Status == WorkflowState.Completed || Status == WorkflowState.PartiallyCompleted;
    public TimeSpan Duration { get; private set; }
    public int TotalSteps { get; private set; }
    public int CompletedSteps { get; private set; }
    public int FailedSteps { get; private set; }
    public int SkippedSteps { get; private set; }
    public int BatchCount { get; private set; }
    public int RetryCount { get; private set; }
    public string? Error { get; private set; }
    public Dictionary<string, string> OutputData { get; private set; } = new();

    private WorkflowResult() { }

    public static WorkflowResult Success(
        string workflowId,
        WorkflowState status,
        TimeSpan duration,
        int totalSteps,
        int completedSteps,
        int failedSteps,
        int skippedSteps,
        int batchCount,
        int retryCount,
        IReadOnlyDictionary<string, string> outputData)
    {
        return new WorkflowResult
        {
            WorkflowId = workflowId,
            Status = status,
            Duration = duration,
            TotalSteps = totalSteps,
            CompletedSteps = completedSteps,
            FailedSteps = failedSteps,
            SkippedSteps = skippedSteps,
            BatchCount = batchCount,
            RetryCount = retryCount,
            OutputData = new Dictionary<string, string>(outputData)
        };
    }

    public static WorkflowResult Failure(
        string workflowId,
        TimeSpan duration,
        string error,
        int totalSteps,
        int completedSteps,
        int failedSteps)
    {
        return new WorkflowResult
        {
            WorkflowId = workflowId,
            Status = WorkflowState.Failed,
            Duration = duration,
            Error = error,
            TotalSteps = totalSteps,
            CompletedSteps = completedSteps,
            FailedSteps = failedSteps
        };
    }
}
