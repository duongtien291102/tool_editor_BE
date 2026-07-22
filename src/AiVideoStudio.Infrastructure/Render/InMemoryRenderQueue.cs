using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Application.Features.RenderJobs.DTOs;
using AiVideoStudio.Application.Interfaces.Render;

namespace AiVideoStudio.Infrastructure.Render;

/// <summary>
/// A lightweight, thread-safe in-memory priority queue.
/// Does NOT use Channel to allow Priority sorting and Peek/Remove operations.
/// </summary>
public class InMemoryRenderQueue : IRenderQueue
{
    private readonly List<QueueItem> _queue = new();
    private readonly SemaphoreSlim _semaphore = new(0);
    private readonly object _lock = new();

    public int Count
    {
        get
        {
            lock (_lock) return _queue.Count;
        }
    }

    public Task EnqueueAsync(QueueItem item, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _queue.Add(item);
        }
        
        // Signal that an item is available
        _semaphore.Release();
        
        return Task.CompletedTask;
    }

    public async Task<QueueItem?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        // Wait asynchronously until an item is available or cancellation is requested
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null; // Graceful exit on cancellation
        }

        lock (_lock)
        {
            if (_queue.Count == 0) return null;

            // Find highest priority, then oldest CreatedAt
            var item = _queue
                .OrderByDescending(x => (int)x.Priority)
                .ThenBy(x => x.CreatedAt)
                .First();

            _queue.Remove(item);
            return item;
        }
    }

    public Task<QueueItem?> PeekAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_queue.Count == 0) return Task.FromResult<QueueItem?>(null);

            var item = _queue
                .OrderByDescending(x => (int)x.Priority)
                .ThenBy(x => x.CreatedAt)
                .First();

            return Task.FromResult<QueueItem?>(item);
        }
    }

    public Task RemoveAsync(string jobId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var item = _queue.FirstOrDefault(x => x.JobId == jobId);
            if (item != null)
            {
                _queue.Remove(item);
                // Try to consume one semaphore count since we removed an item manually
                _semaphore.Wait(0); 
            }
        }
        return Task.CompletedTask;
    }
}
