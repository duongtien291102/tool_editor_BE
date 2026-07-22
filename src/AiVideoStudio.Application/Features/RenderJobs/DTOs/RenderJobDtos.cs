using System;
using System.Text.Json;
using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Application.Features.RenderJobs.DTOs;

/// <summary>Full render job DTO returned to clients.</summary>
public record RenderJobDto(
    string Id,
    string ProjectId,
    string? TimelineId,
    string? ScriptId,
    string OwnerId,
    RenderJobType JobType,
    RenderProvider Provider,
    RenderJobStatus Status,
    RenderPriority Priority,
    int Progress,
    int RetryCount,
    int MaxRetryCount,
    JsonDocument? InputPayload,
    JsonDocument? OutputPayload,
    string? ErrorMessage,
    string? ErrorCode,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

/// <summary>Lightweight DTO for list views.</summary>
public record RenderJobSummaryDto(
    string Id,
    string ProjectId,
    string OwnerId,
    RenderJobType JobType,
    RenderProvider Provider,
    RenderJobStatus Status,
    RenderPriority Priority,
    int Progress,
    int RetryCount,
    int MaxRetryCount,
    string? ErrorMessage,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt
);
