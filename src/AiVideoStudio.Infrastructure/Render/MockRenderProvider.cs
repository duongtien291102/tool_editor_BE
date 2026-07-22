using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Application.Features.RenderJobs.DTOs;
using AiVideoStudio.Application.Interfaces.Render;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AiVideoStudio.Infrastructure.Render;

/// <summary>
/// A mock provider that simulates rendering by delaying execution.
/// Real implementation would call OpenAI, Runway, etc. APIs.
/// </summary>
public class MockRenderProvider : IRenderProvider
{
    private readonly ILogger<MockRenderProvider> _logger;
    private readonly TimeSpan _simulatedDuration = TimeSpan.FromSeconds(5); // Simulated render time

    public string ProviderName => "MockProvider";

    public MockRenderProvider(ILogger<MockRenderProvider> logger)
    {
        _logger = logger;
    }

    public async Task<RenderResult> RenderAsync(RenderJob job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MockRenderProvider starting job {JobId} (Type: {Type})", job.Id, job.JobType);

        var startTime = DateTimeOffset.UtcNow;

        try
        {
            // Simulate work divided into 10 steps (for progress updates if we wanted to push from provider)
            int steps = 10;
            int stepDelayMs = (int)(_simulatedDuration.TotalMilliseconds / steps);

            for (int i = 0; i < steps; i++)
            {
                // Break out early if cancelled
                cancellationToken.ThrowIfCancellationRequested();
                
                await Task.Delay(stepDelayMs, cancellationToken);
            }

            var duration = DateTimeOffset.UtcNow - startTime;

            // Simulate successful output payload
            var outputObj = new
            {
                jobId = job.Id,
                status = "Success",
                mockData = "This is a simulated render result.",
                generatedAt = DateTimeOffset.UtcNow
            };
            var jsonString = JsonSerializer.Serialize(outputObj);

            _logger.LogInformation("MockRenderProvider completed job {JobId}", job.Id);

            return RenderResult.Succeeded(jsonString, duration);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("MockRenderProvider rendering cancelled for job {JobId}", job.Id);
            var duration = DateTimeOffset.UtcNow - startTime;
            return RenderResult.Failed("CANCELLED", "Render was cancelled by the user or system.", duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MockRenderProvider encountered an error on job {JobId}", job.Id);
            var duration = DateTimeOffset.UtcNow - startTime;
            return RenderResult.Failed("SYSTEM_ERROR", ex.Message, duration);
        }
    }

    public Task CancelAsync(string jobId, CancellationToken cancellationToken = default)
    {
        // For the mock provider, cancellation is handled purely via CancellationToken passed to RenderAsync.
        // A real provider might call an external API (e.g., POST /v1/jobs/{id}/cancel).
        _logger.LogInformation("MockRenderProvider received cancel signal for job {JobId}", jobId);
        return Task.CompletedTask;
    }
}
