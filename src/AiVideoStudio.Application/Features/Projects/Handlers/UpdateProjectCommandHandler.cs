using AiVideoStudio.Application.Events;
using AiVideoStudio.Application.Features.Projects.Commands;
using AiVideoStudio.Application.Features.Projects.DTOs;
using AiVideoStudio.Application.Features.Projects.Validators;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Domain.Events.Projects;
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

public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, Result<ProjectDto>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus;
    private readonly UpdateProjectCommandValidator _validator;

    public UpdateProjectCommandHandler(
        IProjectRepository projectRepository,
        ICurrentUser currentUser,
        IMapper mapper,
        IEventBus eventBus)
    {
        _projectRepository = projectRepository;
        _currentUser = currentUser;
        _mapper = mapper;
        _eventBus = eventBus;
        _validator = new UpdateProjectCommandValidator();
    }

    public async Task<Result<ProjectDto>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result<ProjectDto>.Failure(AuthErrors.Unauthorized);
        }

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage);
            return Result<ProjectDto>.Failure(ProjectErrors.NameRequired, errors);
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

        project.Update(request.Name, request.Description, request.Thumbnail, request.Status, _currentUser.UserId);

        await _projectRepository.UpdateAsync(project, cancellationToken);

        await _eventBus.PublishAsync(new ProjectUpdatedEvent(project.Id, _currentUser.UserId), cancellationToken);

        var dto = _mapper.Map<ProjectDto>(project);
        return Result<ProjectDto>.Success(dto);
    }
}
