using System;
using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Application.Features.RenderJobs.DTOs;

/// <summary>
/// Lightweight queue entry. The queue does NOT hold RenderJob aggregates.
/// Worker retrieves the full aggregate from the repository by JobId.
/// </summary>
public record QueueItem(
    string JobId,
    RenderPriority Priority,
    DateTimeOffset CreatedAt
);

/// <summary>Result returned by IRenderProvider after processing.</summary>
public record RenderResult(
    bool IsSuccess,
    string? OutputPayload,
    string? ErrorCode,
    string? ErrorMessage,
    TimeSpan Duration
)
{
    public static RenderResult Succeeded(string? outputPayload, TimeSpan duration)
        => new(true, outputPayload, null, null, duration);

    public static RenderResult Failed(string errorCode, string errorMessage, TimeSpan duration)
        => new(false, null, errorCode, errorMessage, duration);
}
