# Sprint 7 — Export Engine Walkthrough

## Outcome

Sprint 7 delivers a complete mock FFmpeg export engine while preserving all previous module business logic. An export starts from a validated RenderJob reference, resolves timeline content, builds a render graph and typed FFmpeg command model, runs through an independent background worker, and produces a mock output manifest.

## Delivered

- `ExportJob` aggregate with full state machine, retry, cancel, progress, versioning, encoding settings, events, and failure details.
- CQRS commands/queries, DTOs, AutoMapper profile, FluentValidation, Result errors, and repository abstraction.
- Timeline, track, clip, and asset resolvers.
- Render graph nodes, edges, layers, dependencies, and timeline metadata.
- Typed inputs, video/audio/subtitle filters, transitions, overlays, and output options.
- `MockExportProvider` with Preparing/Rendering/Muxing phases, progress, cancellation, timeout, retry, and async mock output creation.
- Independent export queue and hosted worker; no changes to Render Queue, RenderWorker, or AI Provider Framework logic.
- Mongo export repository and DI composition.
- Authorized Export API with complete Swagger response contracts.

## Verification Walkthrough

The unit suite verifies aggregate transitions/version/events, retry/cancel/progress invariants, handlers, authorization results, validators, every resolver, graph construction, typed command construction, pipeline orchestration, provider phases/output/cancellation/timeout/retry, queue behavior, and Mongo repository insertion.

The integration suite verifies Create, Get, List, Retry, Cancel, Unauthorized, Forbidden, NotFound, plus an actual ExportWorker run from queue to Completed with a generated manifest.

Final verified totals:

```text
Unit tests:        207 passed
Integration tests: 51 passed
Total:             258 passed, 0 failed, 0 skipped
```

No FFmpeg process or real external provider was executed. No Git command was used.
