using AiVideoStudio.Application.Events;
using AiVideoStudio.Application.Features.Projects.Commands;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Domain.Events.Projects;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Features.Projects.Handlers;

public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, Result<bool>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _eventBus;

    public DeleteProjectCommandHandler(
        IProjectRepository projectRepository,
        ICurrentUser currentUser,
        IEventBus eventBus)
    {
        _projectRepository = projectRepository;
        _currentUser = currentUser;
        _eventBus = eventBus;
    }

    public async Task<Result<bool>> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result<bool>.Failure(AuthErrors.Unauthorized);
        }

        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);
        if (project == null || project.IsDeleted)
        {
            return Result<bool>.Failure(ProjectErrors.NotFound);
        }

        var isAdmin = _currentUser.Roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase) || r.Equals("Administrator", StringComparison.OrdinalIgnoreCase));
        var isOwner = project.OwnerId == _currentUser.UserId;

        if (!isOwner && !isAdmin)
        {
            return Result<bool>.Failure(ProjectErrors.UnauthorizedAccess);
        }

        project.SoftDelete(_currentUser.UserId);

        await _projectRepository.UpdateAsync(project, cancellationToken);

        await _eventBus.PublishAsync(new ProjectDeletedEvent(project.Id, _currentUser.UserId), cancellationToken);

        return Result<bool>.Success(true);
    }
}
