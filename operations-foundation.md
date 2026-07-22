# Operations Foundation

## Production architecture

Sprint 10 adds an Operations vertical slice without changing the business rules of Auth, Project, Media, Script, Timeline, Render, AI Provider, Export, Storage, or Workflow.

| Layer | Responsibility |
| :--- | :--- |
| Domain | Audit, notification, quota, usage, configuration, and maintenance entities; operational enums, events, and repository contracts |
| Application | MediatR commands/queries, Result-based handlers, owner/admin authorization, DTOs, AutoMapper mappings, FluentValidation, and provider-neutral abstractions |
| Infrastructure | Mongo repositories, health probing, bounded in-process metrics, audit/usage/notification services, signed URLs, fixed-window rate limiting, retention cleanup, and the scheduled maintenance worker |
| API | Operations endpoints, correlation/request IDs, security headers, rate-limit hook, request metrics, and mutation audit hook |

All production settings are obtained through typed Options bound from `appsettings`: System, Storage, Workflow, Render, Export, Notification, Maintenance, Health, and Metrics.

## Health flow

`/live` checks only process liveness. `/ready` and `/health` probe MongoDB with a configured timeout, verify storage accessibility, confirm that the provider registry is populated, and confirm registration of Workflow, Render, and Export workers. A failed readiness component produces HTTP 503 and includes component status and elapsed time.

## Metrics flow

The request metrics middleware classifies API traffic into workflow, render, export, upload, or general API series. It records request/status/failure counts and processing observations. The collector also exposes queue length, worker throughput, retry, and failure series for producers and workers. Collection can be disabled and dynamic-series cardinality is capped by `MetricsOptions`.

## Maintenance flow

An admin command or the configured `MaintenanceWorker` creates and starts a persisted `MaintenanceTask`. The runner applies configured retention to upload sessions, render jobs, workflow executions, export jobs, notifications, audit logs, and usage records, then removes expired temporary files. It persists completion and deleted count, or failure details, and updates maintenance metrics.

## Audit flow

For non-read HTTP operations, middleware derives an audit action, result, resource, user, IP address, correlation ID, and trace ID. `IAuditWriter` persists the record through the audit repository. Audit retrieval is administrator-only and paginated; retention cleanup is policy-driven.

## Notification flow

Commands validate owner/admin access before dispatch. `INotificationDispatcher` creates the aggregate, emits its domain event, persists it, and increments delivery metrics. Users can list only their own notifications and mark owned notifications as read; administrators may operate on behalf of a user.

## Security flow

Correlation middleware accepts or creates `X-Correlation-Id`. Production security middleware adds `X-Request-Id`, content-type, frame, referrer, and content-security headers, then applies the provider-neutral `IRateLimiter` by authenticated user or client IP. Admin roles protect configuration, metrics, audit, and maintenance. Sensitive dynamic configuration values are redacted in DTO mapping. `ISignedUrlService` uses Options-backed HMAC-SHA256 signatures and expiry validation.

## Endpoint matrix

| Method | Route | Access | Purpose |
| :--- | :--- | :--- | :--- |
| GET | `/api/v1/system/health` | Anonymous | Full production health |
| GET | `/api/v1/system/ready` | Anonymous | Traffic readiness |
| GET | `/api/v1/system/live` | Anonymous | Process liveness |
| GET | `/api/v1/system/configuration` | Admin | Redacted dynamic configuration |
| PUT | `/api/v1/system/configuration` | Admin | Upsert dynamic configuration |
| GET | `/api/v1/system/metrics` | Admin | Metrics snapshot |
| GET | `/api/v1/system/audit` | Admin | Paginated audit history |
| GET | `/api/v1/system/notifications` | Authenticated owner | Current-user notifications |
| POST | `/api/v1/system/maintenance` | Admin | Run configured maintenance |

