using System.Collections.Concurrent;
using AiVideoStudio.Application.Interfaces.Export;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AiVideoStudio.Infrastructure.Export;

public sealed class ExportWorker : BackgroundService, IExportJobCanceller
{
    private readonly IServiceProvider _services;
    private readonly IExportQueue _queue;
    private readonly ILogger<ExportWorker> _logger;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _active = new();

    public ExportWorker(IServiceProvider services, IExportQueue queue, ILogger<ExportWorker> logger)
    {
        _services = services;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExportWorker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var item = await _queue.DequeueAsync(stoppingToken);
                await ProcessAsync(item.ExportJobId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "ExportWorker failed while dequeuing an export.");
            }
        }
    }

    private async Task ProcessAsync(string exportJobId, CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IExportJobRepository>();
        var pipeline = scope.ServiceProvider.GetRequiredService<IExportPipeline>();
        var job = await repository.GetByIdAsync(exportJobId, stoppingToken);
        if (job is null || job.Status != ExportStatus.Pending) return;

        using var source = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        if (!_active.TryAdd(exportJobId, source)) return;

        try
        {
            job.Start();
            await repository.UpdateAsync(job, stoppingToken);

            var result = await pipeline.ExecuteAsync(job, async (update, token) =>
            {
                if (update.Status == ExportStatus.Rendering && job.Status == ExportStatus.Preparing)
                    job.MarkRendering();
                if (update.Status == ExportStatus.Muxing && job.Status == ExportStatus.Rendering)
                    job.MarkMuxing();
                job.UpdateProgress(update.Progress);
                await repository.UpdateAsync(job, token);
            }, source.Token);

            if (result.IsSuccess)
                job.Complete(result.OutputPath!);
            else
                job.Fail(result.ErrorMessage ?? "Export provider failed.", result.ErrorCode);
            await repository.UpdateAsync(job, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            var current = await repository.GetByIdAsync(exportJobId, CancellationToken.None) ?? job;
            if (current.Status is not (ExportStatus.Cancelled or ExportStatus.Completed or ExportStatus.Failed))
            {
                current.Cancel("system");
                await repository.UpdateAsync(current, CancellationToken.None);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Export {ExportJobId} failed.", exportJobId);
            if (job.Status is not (ExportStatus.Cancelled or ExportStatus.Completed or ExportStatus.Failed))
            {
                job.Fail(exception.Message, "EXPORT_PIPELINE_ERROR");
                await repository.UpdateAsync(job, CancellationToken.None);
            }
        }
        finally
        {
            _active.TryRemove(exportJobId, out _);
        }
    }

    public void CancelActiveExport(string exportJobId)
    {
        if (_active.TryGetValue(exportJobId, out var source)) source.Cancel();
    }
}
