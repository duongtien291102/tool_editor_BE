# Storage, Asset Delivery & Upload Engine

## Architecture

```text
Upload API -> CQRS -> UploadSession -> ChunkUploadEngine
  -> chunk checksum -> resumable local mock chunks -> streaming merge
  -> whole-file checksum -> IStorageProvider
  -> thumbnail/waveform + metadata -> asset manifest -> MediaAsset
```

The subsystem uses only a local `MockStorageProvider`. No S3, Azure Blob, Cloudinary, or external CDN is called.

## Upload Lifecycle

`UploadSession` moves through `Pending -> Uploading -> Merging -> Completed`, with `Failed -> Retry` and cancellation from non-terminal states. It tracks file identity, byte totals, completed chunk indexes, SHA-256 checksum, paths, version, timestamps, and retry count. Duplicate chunk indexes are idempotent, enabling resume.

## Chunk Engine

Every chunk is SHA-256 verified before storage under the private `.chunks` namespace. Merge reads chunks in index order into an OS temporary file, so large uploads are not buffered wholly in memory. The merged file is hashed again and must match the session checksum before being uploaded to the final project asset path. Temporary chunks are deleted after completion or cancellation.

## Storage Abstraction

`IStorageProvider` supports upload, download, delete, move, copy, exists, read streaming, and temporary URL generation. `MockStorageProvider` implements all operations locally. Storage roots come from validated `StorageOptions`; provider code contains no default path. Canonical path validation prevents traversal outside the configured root.

Replacing Mock with S3/Azure/Cloudinary requires only another `IStorageProvider` registration. CQRS handlers and Media business logic do not change.

## Asset Processing

- `MockThumbnailGenerator`: image thumbnail, video thumbnail, audio waveform artifacts.
- `MockMetadataExtractor`: typed image/video/audio/subtitle metadata.
- `AssetManifestBuilder`: immutable manifest DTO plus persisted JSON manifest.
- Completion creates a normal `MediaAsset` through the existing repository contract.

## API

| Method | Route |
| :--- | :--- |
| POST | `/api/v1/uploads/start` |
| POST | `/api/v1/uploads/{id}/chunk` |
| POST | `/api/v1/uploads/{id}/complete` |
| POST | `/api/v1/uploads/{id}/cancel` |
| POST | `/api/v1/uploads/{id}/retry` |
| GET | `/api/v1/uploads/{id}` |
| GET | `/api/v1/projects/{projectId}/uploads` |

All routes are authorized and all async operations propagate cancellation tokens.
