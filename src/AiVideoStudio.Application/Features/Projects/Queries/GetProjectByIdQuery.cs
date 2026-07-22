using AiVideoStudio.Application.Features.Projects.DTOs;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Projects.Queries;

public record GetProjectByIdQuery(
    string Id
) : IRequest<Result<ProjectDto>>;
