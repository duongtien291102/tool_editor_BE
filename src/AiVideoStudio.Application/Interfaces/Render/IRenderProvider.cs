using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Application.Features.RenderJobs.DTOs;
using AiVideoStudio.Domain.Entities;

namespace AiVideoStudio.Application.Interfaces.Render;

/// <summary>
/// Provider abstraction for executing AI rendering.
/// Implementations: MockRenderProvider, OpenAIProvider, RunwayProvider, etc.
/// </summary>
public interface IRenderProvider
{
    /// <summary>
    /// Execute the rendering for the given job.
    /// Returns a strongly-typed RenderResult (never string, object, or dynamic).
    /// </summary>
    Task<RenderResult> RenderAsync(RenderJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Signal the provider to cancel an in-progress job by ID.
    /// Fire-and-forget pattern; best-effort cancellation.
    /// </summary>
    Task CancelAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>Unique name identifying this provider implementation.</summary>
    string ProviderName { get; }
}
