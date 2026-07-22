using AiVideoStudio.Application.Features.Timelines.DTOs;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Timelines;

public record GetTimelineByProjectQuery(string ProjectId) : IRequest<Result<TimelineDto>>;
public record GetTimelineQuery(string Id) : IRequest<Result<TimelineDto>>;
