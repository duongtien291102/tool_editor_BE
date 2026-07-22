using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Application.Features.Timelines.DTOs;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Shared.Responses;
using AutoMapper;
using MediatR;

namespace AiVideoStudio.Application.Features.Timelines.Handlers;

public class TimelineQueriesHandler : 
    IRequestHandler<GetTimelineByProjectQuery, Result<TimelineDto>>,
    IRequestHandler<GetTimelineQuery, Result<TimelineDto>>
{
    private readonly ITimelineRepository _timelineRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly IProjectRepository _projectRepository;

    public TimelineQueriesHandler(
        ITimelineRepository timelineRepository,
        IMapper mapper,
        ICurrentUser currentUser,
        IProjectRepository projectRepository)
    {
        _timelineRepository = timelineRepository;
        _mapper = mapper;
        _currentUser = currentUser;
        _projectRepository = projectRepository;
    }

    private async Task<bool> IsAuthorizedAsync(string projectId, CancellationToken cancellationToken)
    {
        if (_currentUser.Roles.Contains("Admin") || _currentUser.Roles.Contains("Administrator"))
            return true;

        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project == null) return false;

        return project.OwnerId == _currentUser.UserId;
    }

    public async Task<Result<TimelineDto>> Handle(GetTimelineByProjectQuery request, CancellationToken cancellationToken)
    {
        var authorized = await IsAuthorizedAsync(request.ProjectId, cancellationToken);
        if (!authorized)
            return Result<TimelineDto>.Failure(AuthErrors.Unauthorized);

        var timeline = await _timelineRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        if (timeline == null)
            return Result<TimelineDto>.Failure(TimelineErrors.NotFound);

        return Result<TimelineDto>.Success(_mapper.Map<TimelineDto>(timeline));
    }

    public async Task<Result<TimelineDto>> Handle(GetTimelineQuery request, CancellationToken cancellationToken)
    {
        var timeline = await _timelineRepository.GetByIdAsync(request.Id, cancellationToken);
        if (timeline == null)
            return Result<TimelineDto>.Failure(TimelineErrors.NotFound);

        var authorized = await IsAuthorizedAsync(timeline.ProjectId, cancellationToken);
        if (!authorized)
            return Result<TimelineDto>.Failure(AuthErrors.Unauthorized);

        return Result<TimelineDto>.Success(_mapper.Map<TimelineDto>(timeline));
    }
}
