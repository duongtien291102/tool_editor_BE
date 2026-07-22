# Sprint 8 — Storage, Asset Delivery & Upload Engine Walkthrough

Sprint 8 adds a resumable, checksum-safe upload engine without changing prior module business logic. Upload sessions accept independently validated chunks, support idempotent resume, stream-merge large files, verify the complete SHA-256, store through a provider abstraction, produce mock derivatives and metadata, persist an asset manifest, and create the completed MediaAsset.

Delivered components include the UploadSession aggregate/events/repository, five commands and two queries, DTOs/mapping/validators, expanded storage operations, complete mock provider, chunk engine, mock thumbnail/metadata services, manifest builder, Mongo persistence, DI registration, and seven authorized API routes.

Verification covers state/version/events, chunk validation/resume/merge/checksum, every storage operation, streaming reads, temporary URLs, thumbnail/waveform, metadata, manifest, validators, repository, and HTTP Start/Chunk/Resume/Merge/Complete/Cancel/Retry/Get/List/Unauthorized/Forbidden/NotFound.

Final suite: 219 unit tests and 59 integration tests, totaling 278 passed with zero failures or skips. No cloud storage or Git command was used.
