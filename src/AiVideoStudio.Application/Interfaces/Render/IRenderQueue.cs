using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Application.Features.RenderJobs.DTOs;

namespace AiVideoStudio.Application.Interfaces.Render;

/// <summary>
/// Abstraction for the render job queue.
/// Does NOT depend on the RenderJob aggregate — only on QueueItem DTO.
/// Implementations: InMemoryRenderQueue, RabbitMQ, Azure Service Bus, Redis Streams, etc.
/// Dequeue order: Priority DESC, then CreatedAt ASC (FIFO within same priority).
/// </summary>
public interface IRenderQueue
{
    /// <summary>Add a job to the queue.</summary>
    Task EnqueueAsync(QueueItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove and return the highest-priority oldest item.
    /// Returns null if the queue is empty (non-blocking).
    /// </summary>
    Task<QueueItem?> DequeueAsync(CancellationToken cancellationToken = default);

    /// <summary>Peek at the next item without removing it.</summary>
    Task<QueueItem?> PeekAsync(CancellationToken cancellationToken = default);

    /// <summary>Remove a specific item by JobId (for cancellation scenarios).</summary>
    Task RemoveAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>Current approximate queue depth.</summary>
    int Count { get; }
}
