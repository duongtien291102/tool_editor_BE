using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Domain.Entities.Orchestration;

public sealed class OrchestrationStep
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Name { get; private set; } = string.Empty;
    public WorkflowStepType Type { get; private set; }
    public WorkflowStepStatus Status { get; private set; } = WorkflowStepStatus.Pending;

    // Hierarchy & Dependencies
    public string? ParentId { get; private set; }
    public List<string> Children { get; private set; } = new();
    public List<string> DependsOn { get; private set; } = new();
    public string? ParallelGroupId { get; private set; }
    public string? SequentialGroupId { get; private set; }

    // Conditional Execution
    public string? Condition { get; private set; }

    // Batching & Provider parameters
    public string? Provider { get; private set; }
    public string? Resolution { get; private set; }
    public string? Style { get; private set; }
    public string? AspectRatio { get; private set; }
    public string? Model { get; private set; }
    public string? RenderJobId { get; private set; }

    // Retry & Compensation
    public int TimeoutSeconds { get; private set; } = 60;
    public int MaxRetries { get; private set; } = 3;
    public int RetryCount { get; private set; }
    public bool IsCompensated { get; private set; }
    public string? CompensationAction { get; private set; }

    // Context & Error
    public Dictionary<string, string> InputContext { get; private set; } = new();
    public Dictionary<string, string> OutputContext { get; private set; } = new();
    public string? Error { get; private set; }

    private OrchestrationStep() { }

    public OrchestrationStep(
        string name,
        WorkflowStepType type,
        IEnumerable<string>? dependsOn = null,
        string? parentId = null,
        IEnumerable<string>? children = null,
        string? parallelGroupId = null,
        string? sequentialGroupId = null,
        string? condition = null,
        string? provider = null,
        string? resolution = null,
        string? style = null,
        string? aspectRatio = null,
        string? model = null,
        int timeoutSeconds = 60,
        int maxRetries = 3,
        IDictionary<string, string>? inputs = null,
        string? id = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Step name is required.", nameof(name));
        if (timeoutSeconds <= 0 || maxRetries < 0) throw new ArgumentOutOfRangeException();

        Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id;
        Name = name;
        Type = type;
        DependsOn = dependsOn?.Distinct().ToList() ?? new();
        ParentId = parentId;
        Children = children?.Distinct().ToList() ?? new();
        ParallelGroupId = parallelGroupId;
        SequentialGroupId = sequentialGroupId;
        Condition = condition;
        Provider = provider;
        Resolution = resolution;
        Style = style;
        AspectRatio = aspectRatio;
        Model = model;
        TimeoutSeconds = timeoutSeconds;
        MaxRetries = maxRetries;
        InputContext = inputs is null ? new() : new(inputs);
    }

    public void Wait() => Status = WorkflowStepStatus.Waiting;

    public void Start()
    {
        if (Status is not (WorkflowStepStatus.Pending or WorkflowStepStatus.Waiting))
            throw new InvalidOperationException($"Cannot start step '{Name}' from state '{Status}'.");

        Status = WorkflowStepStatus.Running;
        Error = null;
    }

    public void Complete(IDictionary<string, string>? output = null)
    {
        if (Status != WorkflowStepStatus.Running)
            throw new InvalidOperationException($"Cannot complete step '{Name}' when not running.");

        Status = WorkflowStepStatus.Completed;
        if (output is not null)
        {
            OutputContext = new Dictionary<string, string>(output);
        }
    }

    public void Fail(string error)
    {
        if (Status != WorkflowStepStatus.Running)
            throw new InvalidOperationException($"Cannot fail step '{Name}' when not running.");

        Status = WorkflowStepStatus.Failed;
        Error = error;
    }

    public bool CanRetry() => Status == WorkflowStepStatus.Failed && RetryCount < MaxRetries;

    public bool Retry()
    {
        if (!CanRetry()) return false;
        RetryCount++;
        Status = WorkflowStepStatus.Pending;
        Error = null;
        return true;
    }

    public void Skip()
    {
        if (Status is WorkflowStepStatus.Completed or WorkflowStepStatus.Running)
            throw new InvalidOperationException($"Cannot skip step '{Name}' in state '{Status}'.");

        Status = WorkflowStepStatus.Skipped;
    }

    public void Cancel()
    {
        if (Status is not (WorkflowStepStatus.Completed or WorkflowStepStatus.Skipped))
        {
            Status = WorkflowStepStatus.Cancelled;
        }
    }

    public void Compensate(string actionTaken)
    {
        IsCompensated = true;
        CompensationAction = actionTaken;
    }

    public void SetRenderJobId(string jobId) => RenderJobId = jobId;

    public void SetFallbackProvider(string fallbackProvider)
    {
        Provider = fallbackProvider;
    }

    public void Reset()
    {
        Status = WorkflowStepStatus.Pending;
        RetryCount = 0;
        Error = null;
        IsCompensated = false;
        CompensationAction = null;
        OutputContext.Clear();
    }
}
