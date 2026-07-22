using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Application.Features.Exports.DTOs;

public record ExportJobDto(
    string Id,
    string RenderJobId,
    string ProjectId,
    string TimelineId,
    string OwnerId,
    ExportStatus Status,
    int Progress,
    string? OutputPath,
    TimeSpan Duration,
    string Resolution,
    double FrameRate,
    VideoCodec VideoCodec,
    AudioCodec AudioCodec,
    ContainerFormat Container,
    int RetryCount,
    int MaxRetryCount,
    string? ErrorCode,
    string? ErrorMessage,
    int Version,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record ExportSummaryDto(
    string Id,
    string RenderJobId,
    string ProjectId,
    ExportStatus Status,
    int Progress,
    string? OutputPath,
    string Resolution,
    VideoCodec VideoCodec,
    ContainerFormat Container,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
