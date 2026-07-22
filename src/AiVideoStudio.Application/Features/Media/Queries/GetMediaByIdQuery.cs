using AiVideoStudio.Application.Features.Media.DTOs;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Media.Queries;

public record GetMediaByIdQuery(
    string Id
) : IRequest<Result<MediaDto>>;
