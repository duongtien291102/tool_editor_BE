using AiVideoStudio.Application.Events;
using AiVideoStudio.Application.Features.Projects.Commands;
using AiVideoStudio.Application.Features.Projects.DTOs;
using AiVideoStudio.Application.Features.Projects.Validators;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Events.Projects;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using AutoMapper;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Features.Projects.Handlers;

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Result<ProjectDto>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus;
    private readonly CreateProjectCommandValidator _validator;

    public CreateProjectCommandHandler(
        IProjectRepository projectRepository,
        ICurrentUser currentUser,
        IMapper mapper,
        IEventBus eventBus)
    {
        _projectRepository = projectRepository;
        _currentUser = currentUser;
        _mapper = mapper;
        _eventBus = eventBus;
        _validator = new CreateProjectCommandValidator();
    }

    public async Task<Result<ProjectDto>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
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

        var project = Project.Create(
            request.Name,
            _currentUser.UserId,
            request.Description,
            request.Thumbnail);

        await _projectRepository.AddAsync(project, cancellationToken);

        await _eventBus.PublishAsync(new ProjectCreatedEvent(project.Id, project.OwnerId), cancellationToken);

        var dto = _mapper.Map<ProjectDto>(project);
        return Result<ProjectDto>.Success(dto);
    }
}
