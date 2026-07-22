using AiVideoStudio.Application.Features.Projects.DTOs;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Projects.Commands;

public record UpdateProjectCommand(
    string Id,
    string Name,
    string? Description = null,
    string? Thumbnail = null,
    ProjectStatus? Status = null
) : IRequest<Result<ProjectDto>>;
