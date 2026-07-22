# Architecture Review — Sprint 10 Operations Foundation

## Clean Architecture boundaries

- Domain owns operational aggregates, enums, domain events, and persistence contracts.
- Application owns CQRS contracts and handlers, Result errors, DTOs, mappings, validators, owner/admin decisions, and provider-neutral service abstractions.
- Infrastructure owns Mongo repositories, dependency probes, metrics storage, operational dispatchers, signed URLs, rate limiting, maintenance execution, and worker lifecycle.
- API owns HTTP contracts, Swagger descriptions, response mapping, and middleware composition.

Dependencies point inward and production-provider details do not leak into the domain or application. Configuration is bound through typed Options; sensitive dynamic values are redacted at the application mapping boundary.

## Operational reliability

Liveness avoids external calls, while readiness uses a bounded timeout and reports individual dependencies. Metrics are thread-safe, can be disabled, and cap dynamic series. Maintenance persists its lifecycle around retention work and records both cleanup throughput and failures. Correlation ID, request ID, trace ID, and audit metadata create a consistent diagnostic chain.

## Security and authorization

Owner checks protect notification and usage operations. Admin checks protect audit, configuration, metrics, and maintenance. The API adds defensive response headers and a provider-neutral rate-limit hook. Signed URLs are HMAC-based, expiry-bound, and configured outside code.

## Compatibility

Sprint 10 extends dependency injection and the HTTP pipeline but does not change business logic in Auth, Project, Media, Script, Timeline, Render, AI Provider, Export, Storage, or Workflow. Existing regression tests remain part of the full solution run.
