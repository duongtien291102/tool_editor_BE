using AiVideoStudio.Application.Features.Projects.DTOs;
using AiVideoStudio.Application.Features.Projects.Queries;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using AutoMapper;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Features.Projects.Handlers;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, Result<ProjectDto>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public GetProjectByIdQueryHandler(
        IProjectRepository projectRepository,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _projectRepository = projectRepository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<ProjectDto>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result<ProjectDto>.Failure(AuthErrors.Unauthorized);
        }

        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);
        if (project == null || project.IsDeleted)
        {
            return Result<ProjectDto>.Failure(ProjectErrors.NotFound);
        }

        var isAdmin = _currentUser.Roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase) || r.Equals("Administrator", StringComparison.OrdinalIgnoreCase));
        var isOwner = project.OwnerId == _currentUser.UserId;

        if (!isOwner && !isAdmin)
        {
            return Result<ProjectDto>.Failure(ProjectErrors.UnauthorizedAccess);
        }

        var dto = _mapper.Map<ProjectDto>(project);
        return Result<ProjectDto>.Success(dto);
    }
}
