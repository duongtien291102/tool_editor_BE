# Architecture Review — Sprint 9 Workflow Engine

## Clean Architecture boundaries

- Domain owns `AIWorkflow`, steps, executions, variables, templates, triggers, state transitions, DAG invariants, and workflow domain events.
- Application owns CQRS contracts/handlers, Result errors, DTOs, mappings, validators, authorization, and the step-dispatch abstraction.
- Infrastructure owns DAG resolution, scheduling, execution, capability dispatch, worker lifecycle, Mongo repositories, and indexes.
- API owns only HTTP request/response mapping and Swagger metadata.

## Execution and recovery

The scheduler deduplicates IDs and never blocks a request thread. The background worker starts execution tasks with cancellation support. Only nodes whose dependencies reached a successful terminal state are considered. Conditions can skip a node; outputs are namespaced by step ID and also exposed to following steps. Each attempt has its own timeout token. Failures retry up to `MaxRetries`, then fail both workflow and execution. Cancellation cascades to non-terminal steps. Pause is persisted and polled between execution boundaries; resume continues the same execution.

## Provider boundary

Workflow step type maps to a capability, not to a provider. Resolution filters `IRenderProviderRegistry` by capability and `IProviderHealthChecker`, then obtains the implementation from `IRenderProviderFactory`. Only existing mocks are registered. Render/export/custom orchestration remains behind a dispatcher result and does not alter prior queues or aggregates.

## Persistence and compatibility

Workflows and executions are separate Mongo collections. Workflow indexes cover `ProjectId`, `OwnerId`, `Status`, `CreatedAt`, and `UpdatedAt`; execution indexes cover workflow chronology and status. Sprint 9 is additive. Auth, Project, Media, Script, Timeline, Render, Provider, Export, and Storage business logic remains unchanged.
