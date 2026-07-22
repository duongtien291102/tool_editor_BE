# AI Workflow Engine

## Architecture

`AIWorkflow` is the aggregate root and owns a versioned DAG of `WorkflowStep` nodes plus workflow variables. `WorkflowExecution` stores the lifecycle and final output context of a run. Commands and queries cross the Application boundary; Mongo repositories, resolver, scheduler, executor, dispatcher, and background worker implement the runtime in Infrastructure.

## Execution flow

1. API validates and persists a DAG in `Ready` state.
2. Run schedules the workflow ID and immediately returns HTTP 202.
3. `WorkflowWorker` dequeues the ID; `WorkflowExecutor` creates an execution and marks the workflow `Running`.
4. `WorkflowResolver` selects pending/waiting nodes whose dependencies succeeded.
5. A false condition marks the node `Skipped`; otherwise it becomes `Running` and emits a step-start event.
6. The dispatcher executes with a linked per-step timeout token.
7. Outputs are persisted on the node and merged into workflow context as both `stepId.key` and `key`.
8. The loop persists every boundary until all nodes complete/skip, cancellation occurs, or an unrecoverable failure occurs.

## DAG resolution

Creation/update rejects unknown dependency IDs, duplicate step IDs, and cycles. Ready-node selection is deterministic over stored step order. Completed and intentionally skipped dependencies are treated as successful terminal nodes so conditional branches can converge. A graph that cannot progress fails safely rather than spinning.

## Provider selection

AI operations map to `ProviderCapability` values. The dispatcher:

1. enumerates registered provider descriptors;
2. filters by required capability and health;
3. asks `IRenderProviderFactory` for the selected registered implementation;
4. invokes the existing mock provider contract.

There is no provider-name branch, provider-specific dependency, prompt template, or real AI call in the workflow module.

## Retry strategy and failure recovery

Each node owns `MaxRetries` and `RetryCount`. A timeout or execution exception transitions the node to `Failed`; if budget remains it resets to `Pending`, otherwise the workflow and execution become `Failed`. The retry command resets failed/cancelled nodes on a failed workflow and requeues it. Cancel signals an active token and cascades cancellation to unfinished nodes. Pause is persistent and takes effect at execution boundaries; resume clears the flag without creating a new execution.

## Persistence

`aiWorkflows` and `workflowExecutions` are independent Mongo collections. Startup initialization creates indexes for project/owner/status/time queries and execution history. Repository contracts stay in Domain so alternative persistence can be introduced without changing handlers or the engine.
