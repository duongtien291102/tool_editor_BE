using System.Text.Json;
using AiVideoStudio.Application.Features.RenderJobs.DTOs;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.RenderJobs;

// ─────────────────────────────────────────────────────────────────────────────
// Commands
// ─────────────────────────────────────────────────────────────────────────────

public record CreateRenderJobCommand(
    string ProjectId,
    RenderJobType JobType,
    RenderProvider Provider,
    RenderPriority Priority,
    int MaxRetryCount,
    string? TimelineId,
    string? ScriptId,
    JsonDocument? InputPayload
) : IRequest<Result<RenderJobDto>>;

public record CancelRenderJobCommand(
    string JobId
) : IRequest<Result>;

public record RetryRenderJobCommand(
    string JobId
) : IRequest<Result<RenderJobDto>>;

/// <summary>Internal command used by RenderWorker to update progress.</summary>
public record UpdateRenderProgressCommand(
    string JobId,
    int Progress
) : IRequest<Result>;
