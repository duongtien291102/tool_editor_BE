using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Events.Workflows;

public abstract record OrchestrationEvent : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public record OrchestrationWorkflowCreatedEvent(string WorkflowId, string ProjectId, string OwnerId) : OrchestrationEvent;
public record WorkflowQueuedEvent(string WorkflowId, string ProjectId) : OrchestrationEvent;
public record OrchestrationWorkflowStartedEvent(string WorkflowId, string ExecutionId) : OrchestrationEvent;
public record OrchestrationWorkflowStepStartedEvent(string WorkflowId, string StepId) : OrchestrationEvent;
public record OrchestrationWorkflowStepCompletedEvent(string WorkflowId, string StepId) : OrchestrationEvent;
public record OrchestrationWorkflowStepFailedEvent(string WorkflowId, string StepId, string Error) : OrchestrationEvent;
public record OrchestrationWorkflowCompletedEvent(string WorkflowId, string ProjectId) : OrchestrationEvent;
public record OrchestrationWorkflowCancelledEvent(string WorkflowId, string Reason) : OrchestrationEvent;
public record OrchestrationWorkflowFailedEvent(string WorkflowId, string Error) : OrchestrationEvent;
public record WorkflowCompensatedEvent(string WorkflowId, string StepId, string ActionTaken) : OrchestrationEvent;
