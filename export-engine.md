# Export Engine — Mock FFmpeg Pipeline

## Scope

Sprint 7 provides a complete export architecture without invoking FFmpeg or any external render service. The current provider writes a JSON manifest using the requested container extension; this is a deterministic mock output, not encoded media.

## End-to-End Flow

```text
RenderJob reference
  -> CreateExportJobCommand
  -> ExportJob + IExportQueue
  -> ExportWorker
  -> IExportPipeline
      -> ITimelineResolver
      -> ITrackResolver
      -> IClipResolver
      -> IAssetResolver
      -> IExportGraphBuilder
      -> IFFmpegCommandBuilder
      -> IExportProvider
  -> MockExportProvider
  -> configured output manifest
```

`IExportQueue` and `ExportWorker` are independent from `IRenderQueue` and `RenderWorker`. Both hosted workers can run concurrently.

## ExportJob Aggregate

The aggregate owns export state, progress, retry count, timestamps, output details, encoding options, and optimistic version increments.

```text
Pending -> Preparing -> Rendering -> Muxing -> Completed
    |          |            |          |
    +----------+------------+----------+-> Cancelled
               +------------+----------+-> Failed -> Pending (Retry)
```

Progress is monotonic, constrained to `0..99` during processing, and becomes `100` only on completion. Every mutation increments `Version`. Started, progress, completed, failed, and cancelled transitions publish domain events.

## Resolution Pipeline

- `TimelineResolver` loads the requested timeline from `ITimelineRepository`.
- `TrackResolver` returns tracks in timeline order.
- `ClipResolver` flattens clips in start-time order.
- `AssetResolver` resolves each distinct asset through `IMediaAssetRepository` and fails if any dependency is absent.

All operations accept and propagate `CancellationToken`.

## Render Graph

`ExportGraphBuilder` converts timeline data into a vendor-neutral graph:

- `TimelineGraph`: frame rate, resolution, duration.
- `RenderGraphNode`: clip, track, asset, timing, layer, source, metadata.
- `RenderGraphEdge`: sequencing between adjacent nodes.
- `RenderGraphLayer`: ordered track and its node IDs.
- `RenderGraphDependency`: node-to-asset source dependency.

This graph is suitable for a future local, Docker, or cloud command adapter.

## FFmpeg Command Object

`FFmpegCommandBuilder` never creates a shell command. It builds `FFmpegCommandModel` containing:

- input assets;
- video, audio, and subtitle filters;
- transitions and overlays;
- output resolution, frame rate, codecs, container, directory, and file name.

The output directory is supplied by `IOptions<ExportOptions>` and is never embedded in provider source.

## Mock Provider Lifecycle

`MockExportProvider` reports deterministic phases and progress:

1. Preparing: 10%, 25%.
2. Rendering: 45%, 70%.
3. Muxing: 90%, 99%.
4. Serialize the command model as the mock output manifest.

The provider implements linked caller cancellation, configured timeout, bounded retry, timeout/error result mapping, async file I/O, and structured logging. It does not call `Process`, a native executable, Docker, cloud workers, or FFmpeg.

Configuration:

```json
{
  "Export": {
    "OutputDirectory": "./exports",
    "TimeoutSeconds": 60,
    "RetryCount": 1,
    "MockStepDelayMilliseconds": 20
  }
}
```

## API

| Method | Route | Purpose |
| :--- | :--- | :--- |
| POST | `/api/v1/export` | Create and queue export |
| GET | `/api/v1/export/{id}` | Get export details |
| GET | `/api/v1/projects/{projectId}/exports` | List project exports |
| POST | `/api/v1/export/{id}/cancel` | Cancel pending/active export |
| POST | `/api/v1/export/{id}/retry` | Retry failed export |

All endpoints are authorized, use CQRS/Result responses, and document success/error contracts through Swagger metadata.

## Replacing the Mock Provider

To add a real backend later:

1. Implement `IExportProvider` for local FFmpeg, a Docker service, or a cloud worker.
2. Translate `FFmpegCommandModel` inside that adapter only.
3. Preserve progress callback, cancellation, timeout, and typed results.
4. Replace the DI registration for `IExportProvider`.

No controller, handler, graph builder, queue, worker, or Render pipeline change is required.
