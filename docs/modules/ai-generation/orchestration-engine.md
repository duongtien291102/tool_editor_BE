# AI Generation Orchestration Engine

## Architecture Overview
The AI Generation Orchestration Engine is a dedicated Event-Driven orchestration layer responsible for managing the complete lifecycle of AI video generation workflows. 
Crucially, the Orchestration Engine **does not directly generate AI content, does not render, and does not invoke AI providers directly**. Instead, it coordinates scene/shot partitioning, DAG dependency resolution, parallel execution, batch scheduling, retry orchestration, cancellation, compensation, and domain event publishing through the existing Render Engine, Provider Framework, Event Bus, and Persistence layer.

```
Workflow -> Scene -> Shot -> Generation Task -> RenderJob -> Provider -> Completed
```

---

## State Machine
The workflow transitions strictly through controlled states managed by `WorkflowStateMachine`:

```mermaid
stateDiagram-v2
    [*] --> Draft
    Draft --> Queued : Queue()
    Draft --> Cancelled : Cancel()
    Queued --> Running : Start()
    Queued --> Cancelled : Cancel()
    Queued --> Failed : Fail()
    Running --> Waiting : Wait()
    Running --> PartiallyCompleted : PartialComplete()
    Running --> Completed : Complete()
    Running --> Failed : Fail()
    Running --> Cancelled : Cancel()
    Waiting --> Running : Resume()
    Waiting --> Cancelled : Cancel()
    Waiting --> Failed : Fail()
    PartiallyCompleted --> Running : Resume()
    PartiallyCompleted --> Completed : Complete()
    PartiallyCompleted --> Failed : Fail()
    PartiallyCompleted --> Cancelled : Cancel()
    Failed --> Queued : Retry() / Resume()
    Failed --> Draft : Reset()
    Failed --> Cancelled : Cancel()
    Cancelled --> Queued : Retry()
    Cancelled --> Draft : Reset()
```

---

## Execution Flow & Sequence Diagram
1. **Creation**: `GenerationWorkflow.Create()` builds the aggregate root and validates the graph using `ValidateDag()`. Domain event `OrchestrationWorkflowCreatedEvent` is raised.
2. **Queueing**: `QueueWorkflowAsync()` transitions the state to `Queued` and publishes `WorkflowQueuedEvent`.
3. **Worker Polling**: `GenerationWorkflowWorker` polls `generation_workflows` for `Queued` state items.
4. **Execution & Scheduling**:
   - `WorkflowSchedulerEngine` analyzes the DAG, identifies ready steps, groups parallel steps (e.g. Scene 1 -> Shot A, Shot B, Shot C concurrently), and batches shots by matching attributes (`Provider`, `Resolution`, `Style`, `AspectRatio`, `Model`) up to `WorkflowPolicy.BatchSize`.
   - `OrchestrationDispatcher` dispatches steps/batches through `IWorkflowStepDispatcher`.
5. **Retry & Compensation Strategy**:
   - Step failure attempts retry up to `MaxRetries`.
   - If configured, switches to `ProviderFallback`.
   - On retry exhaustion, executes compensation (`ExecuteCompensationAsync`), rolling back resources or cancelling downstream steps.
   - Respects `Policy.ContinueOnFailure` to allow partial execution.

```mermaid
sequenceDiagram
    participant API as API Controller
    participant Orch as GenerationOrchestrator
    participant Sched as WorkflowSchedulerEngine
    participant Disp as OrchestrationDispatcher
    participant Repo as MongoDB Repository
    participant Bus as EventBus

    API->>Orch: CreateWorkflowAsync()
    Orch->>Repo: AddAsync(workflow)
    Orch->>Bus: Publish(OrchestrationWorkflowCreatedEvent)
    API->>Orch: QueueWorkflowAsync()
    Orch->>Repo: UpdateAsync(workflow)
    Orch->>Bus: Publish(WorkflowQueuedEvent)
    Worker->>Orch: ExecuteWorkflowAsync()
    Orch->>Sched: GetReadySteps() & ScheduleBatches()
    loop Step Execution
        Orch->>Disp: DispatchStepAsync()
        Disp-->>Orch: Step Output
        Orch->>Repo: UpdateAsync()
        Orch->>Bus: Publish(OrchestrationWorkflowStepCompletedEvent)
    end
    Orch->>Repo: UpdateAsync(Completed)
    Orch->>Bus: Publish(OrchestrationWorkflowCompletedEvent)
```

---

## Class Diagram

```mermaid
classDiagram
    class GenerationWorkflow {
        +string Id
        +string ProjectId
        +string OwnerId
        +WorkflowState State
        +WorkflowPolicy Policy
        +WorkflowExecutionContext Context
        +IReadOnlyCollection~OrchestrationStep~ Steps
        +IReadOnlyCollection~WorkflowHistory~ History
        +ValidateDag()
        +Queue()
        +Start()
        +Complete()
        +Fail()
        +Cancel()
        +Retry()
        +Resume()
    }

    class OrchestrationStep {
        +string Id
        +string Name
        +WorkflowStepType Type
        +WorkflowStepStatus Status
        +List~string~ DependsOn
        +string Provider
        +string Resolution
        +string Style
        +string AspectRatio
        +string Model
        +int MaxRetries
        +int RetryCount
        +Start()
        +Complete()
        +Fail()
        +Retry()
        +Compensate()
    }

    class WorkflowPolicy {
        +int MaxRetry
        +bool ContinueOnFailure
        +int Parallelism
        +int BatchSize
        +string ProviderFallback
        +int TimeoutSeconds
        +string Cancellation
    }

    class WorkflowStateMachine {
        +CanTransition()
        +ValidateTransition()
    }

    GenerationWorkflow "1" *-- "many" OrchestrationStep
    GenerationWorkflow "1" *-- "1" WorkflowPolicy
    GenerationWorkflow ..> WorkflowStateMachine
```

---

## Persistence & Metrics
- **MongoDB Collections**: `generation_workflows`, `workflow_steps`, `workflow_histories`.
- **Health Check**: `WorkflowOrchestratorHealthCheck` inspecting queue depth and engine status.
- **OpenTelemetry Metrics**:
  - `workflow_started_total`
  - `workflow_completed_total`
  - `workflow_failed_total`
  - `workflow_duration_seconds`
  - `workflow_parallel_steps_total`
  - `workflow_batch_total`
  - `workflow_retry_total`
