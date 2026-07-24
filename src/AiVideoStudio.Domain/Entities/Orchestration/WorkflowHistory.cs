using AiVideoStudio.Domain.Base;
using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Domain.Entities.Orchestration;

public sealed class WorkflowHistory : BaseEntity
{
    public string WorkflowId { get; private set; } = string.Empty;
    public string? StepId { get; private set; }
    public WorkflowState State { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string? Details { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public DateTimeOffset Timestamp { get; private set; } = DateTimeOffset.UtcNow;

    private WorkflowHistory() { }

    public static WorkflowHistory Record(
        string workflowId,
        WorkflowState state,
        string message,
        string? stepId = null,
        string? details = null,
        string? correlationId = null)
    {
        return new WorkflowHistory
        {
            WorkflowId = workflowId,
            StepId = stepId,
            State = state,
            Message = message,
            Details = details,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
