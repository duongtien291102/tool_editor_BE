# Sprint 9 — AI Workflow Orchestration Engine Walkthrough

Sprint 9 adds a provider-neutral orchestration layer without calling real AI services or changing the business logic delivered in earlier sprints. A workflow is a validated directed acyclic graph (DAG); each step declares dependencies, an optional condition, timeout, retry limit, input context, and capability-oriented operation.

The application exposes CQRS commands and queries through MediatR, Result, FluentValidation, AutoMapper, and owner/admin authorization. Eleven authorized HTTP endpoints cover create, read, list, update, delete, run, cancel, retry, pause, resume, and latest execution.

`WorkflowWorker` consumes the non-blocking scheduler in the background. `WorkflowExecutor` persists an execution, resolves ready nodes, evaluates conditions, updates step state, propagates output context, enforces timeouts and retries, observes pause/cancellation, persists after each boundary, and records domain events. AI-capable steps request a `ProviderCapability`; the dispatcher discovers a healthy registered provider and resolves it through the existing `IRenderProviderFactory`. No provider name, provider switch, or prompt is embedded in the workflow engine.

Mongo persistence includes workflow and execution repositories plus startup index creation. Verification now contains 241 unit tests and 69 integration tests: 310 tests passed with zero failures or skips.
