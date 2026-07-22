using AiVideoStudio.Application.Features.Exports.DTOs;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Exports;

public record CreateExportJobCommand(
    string RenderJobId,
    string ProjectId,
    string TimelineId,
    VideoCodec VideoCodec,
    AudioCodec AudioCodec,
    ContainerFormat Container,
    int MaxRetryCount = 3) : IRequest<Result<ExportJobDto>>;

public record CancelExportJobCommand(string ExportJobId) : IRequest<Result>;
public record RetryExportJobCommand(string ExportJobId) : IRequest<Result<ExportJobDto>>;
public record UpdateExportProgressCommand(string ExportJobId, int Progress) : IRequest<Result>;
