using AiVideoStudio.Application.Features.Media.DTOs;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Media.Commands;

public record UpdateMediaCommand(
    string Id,
    string? FileName = null,
    int? Width = null,
    int? Height = null,
    double? Duration = null,
    string? ThumbnailPath = null
) : IRequest<Result<MediaDto>>;
