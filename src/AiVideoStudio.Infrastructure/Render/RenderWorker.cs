using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Application.Features.RenderJobs;
using AiVideoStudio.Application.Features.RenderJobs.DTOs;
using AiVideoStudio.Application.Interfaces.Render;
using AiVideoStudio.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AiVideoStudio.Infrastructure.Render;

/// <summary>
/// Background worker that dequeues RenderJobs and processes them via IRenderProvider.
/// Supports cancellation, progress updates, and exponential backoff retries.
/// </summary>
public class RenderWorker : BackgroundService, IRenderJobCanceller
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRenderQueue _queue;
    private readonly ILogger<RenderWorker> _logger;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeJobs = new();

    public RenderWorker(
        IServiceProvider serviceProvider,
        IRenderQueue queue,
        ILogger<RenderWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RenderWorker is starting.");

        // Loop continuously until the host shuts down
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Blocking wait for the next item or cancellation
                var item = await _queue.DequeueAsync(stoppingToken);

                if (item != null)
                {
                    // Fire and forget so we can process multiple if we had multiple worker threads.
                    // For now, we await it to process sequentially (WorkerCount = 1 logic).
                    // To scale, we could use Task.Run or SemaphoreSlim to limit concurrency.
                    await ProcessJobAsync(item.JobId, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RenderWorker encountered an unexpected error while dequeuing.");
                await Task.Delay(5000, stoppingToken); // Prevent tight failure loops
            }
        }

        _logger.LogInformation("RenderWorker is stopping.");
        
        // Cancel any actively running jobs
        foreach (var cts in _activeJobs.Values)
        {
            cts.Cancel();
        }
    }

    private async Task ProcessJobAsync(string jobId, CancellationToken hostCancellationToken)
    {
        _logger.LogInformation("RenderWorker dequeued job {JobId}", jobId);

        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRenderJobRepository>();
        var provider = scope.ServiceProvider.GetRequiredService<IRenderProvider>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var job = await repo.GetByIdAsync(jobId, hostCancellationToken);
        if (job == null)
        {
            _logger.LogWarning("RenderWorker could not find job {JobId} in repository.", jobId);
            return;
        }

        if (job.Status == Domain.Enums.RenderJobStatus.Cancelled)
        {
            _logger.LogInformation("Job {JobId} was already cancelled. Skipping.", jobId);
            return;
        }

        // Apply exponential backoff if this is a retry (Wait before starting)
        if (job.RetryCount > 0)
        {
            var backoffSeconds = Math.Pow(2, job.RetryCount - 1); // 1s, 2s, 4s, 8s...
            _logger.LogInformation("Job {JobId} is a retry ({RetryCount}). Backing off for {BackoffSeconds}s.", jobId, job.RetryCount, backoffSeconds);
            await Task.Delay(TimeSpan.FromSeconds(backoffSeconds), hostCancellationToken);
        }

        // Create a linked token so we can cancel individual jobs if requested via API
        using var jobCts = CancellationTokenSource.CreateLinkedTokenSource(hostCancellationToken);
        _activeJobs.TryAdd(jobId, jobCts);

        try
        {
            // Re-fetch job in case it was cancelled during backoff
            job = await repo.GetByIdAsync(jobId, hostCancellationToken);
            if (job == null || job.Status == Domain.Enums.RenderJobStatus.Cancelled)
                return;

            // Transition to Processing
            job.Start();
            await repo.UpdateAsync(job, hostCancellationToken);

            // Periodically report progress via a background task (simulation)
            var progressTask = Task.Run(async () =>
            {
                for (int i = 10; i <= 90; i += 20)
                {
                    if (jobCts.Token.IsCancellationRequested) break;
                    await Task.Delay(1000, jobCts.Token);
                    if (jobCts.Token.IsCancellationRequested) break;
                    
                    // Use mediator to update progress so it goes through handlers/events
                    await mediator.Send(new UpdateRenderProgressCommand(jobId, i), hostCancellationToken);
                }
            }, jobCts.Token);

            // Execute the provider
            var result = await provider.RenderAsync(job, jobCts.Token);

            // Wait for progress reporter to finish
            try { await progressTask; } catch (OperationCanceledException) { }

            // Re-fetch before final update to avoid version conflicts if it was updated externally
            job = await repo.GetByIdAsync(jobId, hostCancellationToken) ?? job;

            if (job.Status == Domain.Enums.RenderJobStatus.Cancelled)
            {
                _logger.LogInformation("Job {JobId} was cancelled during processing.", jobId);
                return;
            }

            if (result.IsSuccess)
            {
                JsonDocument? outputDoc = null;
                if (!string.IsNullOrEmpty(result.OutputPayload))
                {
                    try { outputDoc = JsonDocument.Parse(result.OutputPayload); }
                    catch { /* Fallback to null */ }
                }

                job.Complete(outputDoc);
                _logger.LogInformation("Job {JobId} completed successfully.", jobId);
            }
            else
            {
                job.Fail(result.ErrorMessage ?? "Unknown error", result.ErrorCode);
                _logger.LogWarning("Job {JobId} failed. Error: {Error}", jobId, result.ErrorMessage);
            }

            await repo.UpdateAsync(job, hostCancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Job {JobId} was cancelled via token.", jobId);
            
            // Note: The actual Cancel() transition is handled by the CancelRenderJobCommand handler,
            // we just stop processing here.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} threw an unhandled exception.", jobId);
            
            job = await repo.GetByIdAsync(jobId, hostCancellationToken) ?? job;
            if (job.Status != Domain.Enums.RenderJobStatus.Cancelled)
            {
                job.Fail($"Unhandled exception: {ex.Message}", "SYSTEM_CRASH");
                await repo.UpdateAsync(job, hostCancellationToken);
            }
        }
        finally
        {
            _activeJobs.TryRemove(jobId, out _);
        }
    }

    /// <summary>
    /// Can be called by other components (e.g. CancelRenderJobCommand) to interrupt a running job.
    /// </summary>
    public void CancelActiveJob(string jobId)
    {
        if (_activeJobs.TryGetValue(jobId, out var cts))
        {
            cts.Cancel();
            _logger.LogInformation("Requested cancellation for active job {JobId}.", jobId);
        }
    }
}
