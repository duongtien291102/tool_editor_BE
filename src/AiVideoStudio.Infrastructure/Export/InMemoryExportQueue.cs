using System.Collections.Concurrent;
using AiVideoStudio.Application.Interfaces.Export;

namespace AiVideoStudio.Infrastructure.Export;

public sealed class InMemoryExportQueue : IExportQueue
{
    private readonly ConcurrentQueue<ExportQueueItem> _items = new();
    private readonly ConcurrentDictionary<string, byte> _removed = new();
    private readonly SemaphoreSlim _signal = new(0);

    public ValueTask EnqueueAsync(ExportQueueItem item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _removed.TryRemove(item.ExportJobId, out _);
        _items.Enqueue(item);
        _signal.Release();
        return ValueTask.CompletedTask;
    }

    public async ValueTask<ExportQueueItem> DequeueAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            await _signal.WaitAsync(cancellationToken);
            if (_items.TryDequeue(out var item) && !_removed.TryRemove(item.ExportJobId, out _))
                return item;
        }
    }

    public bool Remove(string exportJobId) => _removed.TryAdd(exportJobId, 0);
}
