using AiVideoStudio.Domain.Base;
namespace AiVideoStudio.Domain.Events.Workflows;
public abstract record WorkflowEvent : IDomainEvent { public DateTimeOffset OccurredOn { get; }=DateTimeOffset.UtcNow; }
public record WorkflowCreatedEvent(string WorkflowId,string OwnerId):WorkflowEvent;
public record WorkflowStartedEvent(string WorkflowId,string ExecutionId):WorkflowEvent;
public record WorkflowStepStartedEvent(string WorkflowId,string StepId):WorkflowEvent;
public record WorkflowStepCompletedEvent(string WorkflowId,string StepId):WorkflowEvent;
public record WorkflowStepFailedEvent(string WorkflowId,string StepId,string Error):WorkflowEvent;
public record WorkflowCompletedEvent(string WorkflowId):WorkflowEvent;
public record WorkflowCancelledEvent(string WorkflowId):WorkflowEvent;
public record WorkflowRetriedEvent(string WorkflowId):WorkflowEvent;
