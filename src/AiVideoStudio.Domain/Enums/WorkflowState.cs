namespace AiVideoStudio.Domain.Enums;

public enum WorkflowState
{
    Draft,
    Queued,
    Running,
    Waiting,
    PartiallyCompleted,
    Completed,
    Cancelled,
    Failed
}
