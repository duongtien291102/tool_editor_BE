using AiVideoStudio.Domain.Base;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Events.Workflows;

namespace AiVideoStudio.Domain.Entities.Orchestration;

public sealed class GenerationWorkflow : BaseEntity
{
    private readonly List<OrchestrationStep> _steps = new();
    private readonly List<WorkflowHistory> _history = new();

    public string ProjectId { get; private set; } = string.Empty;
    public string? SceneId { get; private set; }
    public string? ShotId { get; private set; }
    public string OwnerId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public WorkflowState State { get; private set; } = WorkflowState.Draft;
    public WorkflowPolicy Policy { get; private set; } = new();
    public WorkflowExecutionContext Context { get; private set; } = new();
    public int Version { get; private set; } = 1;
    public string? CurrentExecutionId { get; private set; }
    public string CorrelationId { get; private set; } = Guid.NewGuid().ToString();
    public string? Error { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public IReadOnlyCollection<OrchestrationStep> Steps => _steps.AsReadOnly();
    public IReadOnlyCollection<WorkflowHistory> History => _history.AsReadOnly();

    private GenerationWorkflow() { }

    public static GenerationWorkflow Create(
        string projectId,
        string ownerId,
        string name,
        string? description,
        IEnumerable<OrchestrationStep> steps,
        WorkflowPolicy? policy = null,
        WorkflowExecutionContext? context = null,
        string? sceneId = null,
        string? shotId = null,
        string? correlationId = null)
    {
        if (string.IsNullOrWhiteSpace(projectId)) throw new ArgumentException("ProjectId is required.", nameof(projectId));
        if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("OwnerId is required.", nameof(ownerId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));

        var workflow = new GenerationWorkflow
        {
            ProjectId = projectId,
            OwnerId = ownerId,
            Name = name,
            Description = description,
            SceneId = sceneId,
            ShotId = shotId,
            State = WorkflowState.Draft,
            Policy = policy ?? new WorkflowPolicy(),
            Context = context ?? new WorkflowExecutionContext(),
            CorrelationId = correlationId ?? Guid.NewGuid().ToString(),
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = ownerId
        };

        workflow._steps.AddRange(steps);
        workflow.ValidateDag();

        workflow.RecordHistory(WorkflowState.Draft, "Workflow created in Draft state.");
        workflow.AddDomainEvent(new OrchestrationWorkflowCreatedEvent(workflow.Id, projectId, ownerId));

        return workflow;
    }

    public void ValidateDag()
    {
        var ids = _steps.Select(x => x.Id).ToHashSet();
        if (ids.Count != _steps.Count)
        {
            throw new InvalidOperationException("Workflow step IDs must be unique.");
        }

        // Verify dependencies exist
        foreach (var step in _steps)
        {
            foreach (var dep in step.DependsOn)
            {
                if (!ids.Contains(dep))
                {
                    throw new InvalidOperationException($"Step '{step.Name}' references unknown dependency step ID '{dep}'.");
                }
            }
        }

        // Verify acyclic graph (DAG)
        var visiting = new HashSet<string>();
        var visited = new HashSet<string>();

        bool Visit(string id)
        {
            if (visiting.Contains(id)) return false; // Cycle detected
            if (visited.Contains(id)) return true;

            visiting.Add(id);
            var step = _steps.Single(x => x.Id == id);
            foreach (var dep in step.DependsOn)
            {
                if (!Visit(dep)) return false;
            }

            visiting.Remove(id);
            visited.Add(id);
            return true;
        }

        foreach (var step in _steps)
        {
            if (!Visit(step.Id))
            {
                throw new InvalidOperationException($"Workflow graph contains circular dependency at step '{step.Name}'.");
            }
        }
    }

    public void Queue()
    {
        TransitionTo(WorkflowState.Queued, "Workflow queued for execution.");
        AddDomainEvent(new WorkflowQueuedEvent(Id, ProjectId));
    }

    public void Start(string executionId)
    {
        TransitionTo(WorkflowState.Running, $"Workflow execution started ({executionId}).");
        CurrentExecutionId = executionId;
        Error = null;
        AddDomainEvent(new OrchestrationWorkflowStartedEvent(Id, executionId));
    }

    public void StepStarted(string stepId)
    {
        var step = GetStep(stepId);
        step.Start();
        Touch();
        RecordHistory(State, $"Step '{step.Name}' started.", stepId);
        AddDomainEvent(new OrchestrationWorkflowStepStartedEvent(Id, stepId));
    }

    public void StepCompleted(string stepId, IDictionary<string, string>? output = null)
    {
        var step = GetStep(stepId);
        step.Complete(output);
        if (output is not null)
        {
            Context.Merge(output);
        }

        Touch();
        RecordHistory(State, $"Step '{step.Name}' completed successfully.", stepId);
        AddDomainEvent(new OrchestrationWorkflowStepCompletedEvent(Id, stepId));
    }

    public void StepFailed(string stepId, string error)
    {
        var step = GetStep(stepId);
        step.Fail(error);
        Touch();
        RecordHistory(State, $"Step '{step.Name}' failed: {error}", stepId);
        AddDomainEvent(new OrchestrationWorkflowStepFailedEvent(Id, stepId, error));
    }

    public void StepCompensated(string stepId, string actionTaken)
    {
        var step = GetStep(stepId);
        step.Compensate(actionTaken);
        Touch();
        RecordHistory(State, $"Step '{step.Name}' compensated ({actionTaken}).", stepId);
        AddDomainEvent(new WorkflowCompensatedEvent(Id, stepId, actionTaken));
    }

    public void Complete()
    {
        if (_steps.Any(s => s.Status is not (WorkflowStepStatus.Completed or WorkflowStepStatus.Skipped)))
        {
            throw new InvalidOperationException("Cannot complete workflow while incomplete steps remain.");
        }

        TransitionTo(WorkflowState.Completed, "Workflow completed successfully.");
        CompletedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new OrchestrationWorkflowCompletedEvent(Id, ProjectId));
    }

    public void PartialComplete(string reason)
    {
        TransitionTo(WorkflowState.PartiallyCompleted, $"Workflow partially completed: {reason}");
        CompletedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new OrchestrationWorkflowCompletedEvent(Id, ProjectId));
    }

    public void Fail(string error)
    {
        Error = error;
        TransitionTo(WorkflowState.Failed, $"Workflow execution failed: {error}");
        AddDomainEvent(new OrchestrationWorkflowFailedEvent(Id, error));
    }

    public void Cancel(string reason = "User requested cancellation.")
    {
        TransitionTo(WorkflowState.Cancelled, $"Workflow cancelled: {reason}");
        foreach (var s in _steps)
        {
            s.Cancel();
        }
        AddDomainEvent(new OrchestrationWorkflowCancelledEvent(Id, reason));
    }

    public void Retry()
    {
        if (State != WorkflowState.Failed && State != WorkflowState.Cancelled)
        {
            throw new InvalidOperationException($"Cannot retry workflow in state '{State}'.");
        }

        foreach (var s in _steps.Where(x => x.Status is WorkflowStepStatus.Failed or WorkflowStepStatus.Cancelled))
        {
            s.Reset();
        }

        Error = null;
        Queue();
    }

    public void Resume()
    {
        if (State is not (WorkflowState.Waiting or WorkflowState.Failed or WorkflowState.PartiallyCompleted))
        {
            throw new InvalidOperationException($"Cannot resume workflow in state '{State}'.");
        }

        Queue();
    }

    private OrchestrationStep GetStep(string stepId)
    {
        return _steps.FirstOrDefault(x => x.Id == stepId)
               ?? throw new InvalidOperationException($"Step '{stepId}' not found in workflow '{Id}'.");
    }

    private void TransitionTo(WorkflowState targetState, string message)
    {
        WorkflowStateMachine.ValidateTransition(State, targetState);
        State = targetState;
        Touch();
        RecordHistory(targetState, message);
    }

    private void RecordHistory(WorkflowState state, string message, string? stepId = null, string? details = null)
    {
        _history.Add(WorkflowHistory.Record(Id, state, message, stepId, details, CorrelationId));
    }

    private void Touch()
    {
        Version++;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = OwnerId;
    }
}
