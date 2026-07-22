# Architecture Review — Sprint 7 Export Engine

## Executive Summary

The Export Engine follows Clean Architecture and isolates rendering implementation details behind `IExportProvider`. Domain owns export invariants; Application owns CQRS and vendor-neutral pipeline contracts; Infrastructure owns resolution, graph/command construction, queue/worker execution, Mongo persistence, and the mock adapter; API owns HTTP composition.

## Layer Boundaries

| Layer | Responsibilities |
| :--- | :--- |
| Domain | Export aggregate, statuses, codecs, container, events, repository contract |
| Application | Commands, queries, DTOs, mapping, validation, Result handling, pipeline abstractions and models |
| Infrastructure | Resolvers, graph builder, command builder, mock provider, queue, worker, Mongo repository |
| API | Authorized routes, Swagger contracts, request mapping, HTTP result mapping |

Auth, Project, Media, Script, Timeline, Render Queue, and AI Provider Framework business logic remain unchanged.

## Dependency Flow

Dependencies point inward. Infrastructure consumes Application interfaces and Domain repositories; Application consumes Domain entities and Shared Result contracts. Domain has no Infrastructure/API dependency. `ExportController` dispatches MediatR messages and contains no export business logic.

## Pipeline Review

The pipeline separates content resolution from graph construction and execution. This prevents a future FFmpeg/Docker/cloud adapter from depending directly on repositories or domain aggregates. A provider receives only a complete `FFmpegCommandModel` and progress callback.

The graph explicitly represents node timing, layer ordering, sequence edges, and source dependencies. The command object explicitly represents inputs, filter categories, transitions, overlays, and output settings without shell escaping or executable concerns.

## Concurrency and Cancellation

`ExportWorker` and `RenderWorker` are separate hosted services with separate queues and active-job cancellation registries. Cancellation flows from API handler to worker token, through pipeline and provider, into delay and file operations. Worker state persistence uses its own scoped repository/pipeline and does not affect Render Queue behavior.

## Resilience and Configuration

- Timeout, retry, mock timing, and output directory are validated options.
- Provider retries are bounded and cancellation-aware.
- Missing timelines/assets fail the pipeline deterministically.
- Aggregate progress cannot decrease or reach 100 before completion.
- Terminal states reject invalid transitions.
- Output paths are configuration-derived; no FFmpeg path exists in source.

## Security and API Review

Handlers distinguish unauthenticated (`Unauthorized`) from authenticated non-owner (`Forbidden`) access. Owner/admin checks cover create, get, list, retry, and cancel workflows. Controller routes use `[Authorize]` and publish response types for success, validation, missing resource, unauthorized, and forbidden outcomes.

## Extensibility Assessment

Local FFmpeg, Docker FFmpeg, or a cloud render worker can be introduced by replacing `IExportProvider`. The controller, CQRS handlers, queue, worker, resolver chain, render graph, and Render Pipeline do not require structural changes. The architecture is ready for a real adapter while Sprint 7 intentionally remains mock-only.
