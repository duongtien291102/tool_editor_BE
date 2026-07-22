namespace AiVideoStudio.Application.Interfaces.Export;

public sealed record ExportQueueItem(string ExportJobId, DateTimeOffset CreatedAt);

public interface IExportQueue
{
    ValueTask EnqueueAsync(ExportQueueItem item, CancellationToken cancellationToken = default);
    ValueTask<ExportQueueItem> DequeueAsync(CancellationToken cancellationToken = default);
    bool Remove(string exportJobId);
}

public interface IExportJobCanceller
{
    void CancelActiveExport(string exportJobId);
}
