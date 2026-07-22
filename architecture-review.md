# Architecture Review — Sprint 8 Storage Engine

## Boundaries

Domain owns UploadSession invariants and events. Application owns CQRS, Result handling, validation, DTOs, mappings, and storage-processing contracts. Infrastructure owns local mock persistence, chunk streaming, derived artifacts, metadata, manifests, and Mongo. API only maps HTTP contracts to MediatR.

Existing Media handlers continue using the original `IStorageProvider` methods unchanged; the interface was extended backward-compatibly. Auth, Project, Script, Timeline, Render, AI Provider, and Export logic were not modified.

## Production-readiness

- Per-chunk and complete-file SHA-256 validation.
- Idempotent completed-chunk tracking for resume.
- Disk-streaming merge instead of whole-file memory buffering.
- Cancellation propagated through storage, hashing, copy, merge, and API operations.
- Configured storage root with canonical traversal protection.
- Provider-neutral move/copy/download/stream/delete/temporary URL operations.
- Owner/admin authorization with Unauthorized and Forbidden separation.
- Mongo repository and paginated project queries.

S3, Azure Blob, Cloudinary, and CDN delivery can be introduced behind `IStorageProvider` without changing upload/domain/business workflows.
