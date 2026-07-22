using AiVideoStudio.Application.Features.Projects.DTOs;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Projects.Commands;

public record CreateProjectCommand(
    string Name,
    string? Description = null,
    string? Thumbnail = null
) : IRequest<Result<ProjectDto>>;
