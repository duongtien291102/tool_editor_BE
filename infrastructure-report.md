# Infrastructure & API Report - Sprint 4

## File tạo mới
1. `src/AiVideoStudio.Api/Contracts/V1/Timelines/Requests/TimelineRequests.cs`
   - Defines all Request DTOs for Timeline endpoints: Create, Update, AutoSave, Track operations, and Clip operations.
2. `src/AiVideoStudio.Api/Controllers/v1/TimelinesController.cs`
   - Implements 14 endpoints following RESTful standards.
   - Delegates all business logic to MediatR commands/queries.

## File sửa
1. `src/AiVideoStudio.Infrastructure/Mongo/Repositories/TimelineRepository.cs`
   - Replaced existing stub with full MongoDB integration.
   - Implemented `GetByIdAsync`, `GetByProjectIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`.
   - Added missing repository functionalities: `GetPagedAsync` (Search, Pagination, Sorting), `ExistsByProjectIdAsync`.

## Repository Flow
- **Data Store**: MongoDB. The `Timeline` is stored as a single Aggregate Document inside the `timelines` collection.
- **Operations**:
  - `AddAsync`: Inserts a full document. Participates in Mongo transactions if a session exists.
  - `UpdateAsync`: Replaces the entire document for the aggregate using `ReplaceOneAsync`. Employs Optimistic Concurrency by filtering on `Id` and `Version`. Returns `false` if `ModifiedCount == 0`, allowing the handler to throw a `VersionConflict` error.
  - `DeleteAsync`: Implements Soft Delete by updating the `DeletedAt` field on the aggregate (handled by the Domain entity) and replacing the document.
  - `GetPagedAsync`: Performs dynamic filtering (OwnerId/Admin), text search (regex on Name), pagination, and dynamic sorting.

## API Endpoint Matrix

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/api/v1/timelines` | Create a new timeline | Owner/Admin |
| GET | `/api/v1/timelines/{id}` | Get timeline by ID | Owner/Admin |
| GET | `/api/v1/projects/{projectId}/timeline` | Get timeline by Project | Owner/Admin |
| PUT | `/api/v1/timelines/{id}` | Update timeline (rename, settings) | Owner/Admin |
| DELETE | `/api/v1/timelines/{id}` | Soft delete timeline | Owner/Admin |
| POST | `/api/v1/timelines/{id}/tracks` | Add a new track | Owner/Admin |
| DELETE | `/api/v1/timelines/{id}/tracks/{trackId}` | Remove a track | Owner/Admin |
| PUT | `/api/v1/timelines/{id}/tracks/reorder` | Reorder track position | Owner/Admin |
| POST | `/api/v1/timelines/{id}/clips` | Add a new clip to a track | Owner/Admin |
| PUT | `/api/v1/timelines/{id}/clips/{clipId}` | Update clip properties | Owner/Admin |
| PUT | `/api/v1/timelines/{id}/clips/{clipId}/move` | Move clip to another track | Owner/Admin |
| PUT | `/api/v1/timelines/{id}/clips/{clipId}/resize`| Resize clip duration | Owner/Admin |
| DELETE | `/api/v1/timelines/{id}/clips/{clipId}` | Delete a clip | Owner/Admin |
| POST | `/api/v1/timelines/{id}/autosave` | Auto-save full timeline state | Owner/Admin |

## Swagger Coverage
- **Status Codes**: fully declared using `[ProducesResponseType]` (200 OK, 201 Created, 400 Bad Request, 401 Unauthorized, 404 Not Found, 409 Conflict).
- **Responses**: Returns `ApiResponse<T>` wrappers correctly representing Success and Error states.
- **Attributes**: The entire controller is decorated with `[ApiController]` and `[Route("api/v1")]`.

## Dependency Injection
- `TimelineRepository` is registered dynamically or statically inside `AiVideoStudio.Infrastructure/DependencyInjection.cs`.
- The `TimelinesController` successfully injects `IMediator` from the constructor without depending on any repository directly.
- The pre-existing Dependency Injection module logic was left untouched.

## Optimistic Concurrency Implementation
- The Domain model maintains an internal `Version` field.
- Handlers extract `expectedVersion` from queries or entity state.
- `TimelineRepository.UpdateAsync(timeline, expectedVersion)` enforces filtering on `x => x.Version == expectedVersion`.
- If another process modifies the document before this update, the `expectedVersion` will not match, causing `ModifiedCount` to evaluate to `0`.
- The method returns `false`, enabling `TimelineCommandsHandler` to return `Result.Failure(TimelineErrors.VersionConflict)`.
- Handled properly via `409 Conflict` inside the API Controller error translator.
