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

public class TrackCommandsHandler : 
    IRequestHandler<AddTrackCommand, Result<TrackDto>>,
    IRequestHandler<RemoveTrackCommand, Result>,
    IRequestHandler<ReorderTrackCommand, Result>,
    IRequestHandler<UpdateTrackCommand, Result<TrackDto>>
{
    private readonly ITimelineRepository _timelineRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;

    public TrackCommandsHandler(
        ITimelineRepository timelineRepository,
        IMapper mapper,
        ICurrentUser currentUser)
    {
        _timelineRepository = timelineRepository;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    private async Task<Result<AiVideoStudio.Domain.Entities.Timeline>> GetAndAuthorizeAsync(string timelineId, CancellationToken cancellationToken)
    {
        var timeline = await _timelineRepository.GetByIdAsync(timelineId, cancellationToken);
        if (timeline == null)
            return Result<AiVideoStudio.Domain.Entities.Timeline>.Failure(TimelineErrors.NotFound);

        var userId = _currentUser.UserId;
        if (timeline.OwnerId != userId && !_currentUser.Roles.Contains("Admin") && !_currentUser.Roles.Contains("Administrator"))
            return Result<AiVideoStudio.Domain.Entities.Timeline>.Failure(AuthErrors.Unauthorized);

        return Result<AiVideoStudio.Domain.Entities.Timeline>.Success(timeline);
    }

    public async Task<Result<TrackDto>> Handle(AddTrackCommand request, CancellationToken cancellationToken)
    {
        var timelineResult = await GetAndAuthorizeAsync(request.TimelineId, cancellationToken);
        if (!timelineResult.IsSuccess) return Result<TrackDto>.Failure(timelineResult.Error);

        var timeline = timelineResult.Value;
        var expectedVersion = timeline.Version;
        
        var track = timeline.AddTrack(request.Name, request.TrackType, _currentUser.UserId);

        var updated = await _timelineRepository.UpdateAsync(timeline, expectedVersion, cancellationToken);
        if (!updated)
            return Result<TrackDto>.Failure(TimelineErrors.VersionConflict);

        return Result<TrackDto>.Success(_mapper.Map<TrackDto>(track));
    }

    public async Task<Result> Handle(RemoveTrackCommand request, CancellationToken cancellationToken)
    {
        var timelineResult = await GetAndAuthorizeAsync(request.TimelineId, cancellationToken);
        if (!timelineResult.IsSuccess) return Result.Failure(timelineResult.Error);

        var timeline = timelineResult.Value;
        var expectedVersion = timeline.Version;

        try
        {
            timeline.RemoveTrack(request.TrackId, _currentUser.UserId);
        }
        catch (System.InvalidOperationException ex) when (ex.Message == "TrackContainsClips")
        {
            return Result.Failure(TimelineErrors.TrackContainsClips);
        }

        var updated = await _timelineRepository.UpdateAsync(timeline, expectedVersion, cancellationToken);
        if (!updated)
            return Result.Failure(TimelineErrors.VersionConflict);

        return Result.Success();
    }

    public async Task<Result> Handle(ReorderTrackCommand request, CancellationToken cancellationToken)
    {
        var timelineResult = await GetAndAuthorizeAsync(request.TimelineId, cancellationToken);
        if (!timelineResult.IsSuccess) return Result.Failure(timelineResult.Error);

        var timeline = timelineResult.Value;
        var expectedVersion = timeline.Version;

        timeline.ReorderTrack(request.TrackId, request.NewOrder, _currentUser.UserId);

        var updated = await _timelineRepository.UpdateAsync(timeline, expectedVersion, cancellationToken);
        if (!updated)
            return Result.Failure(TimelineErrors.VersionConflict);

        return Result.Success();
    }

    public async Task<Result<TrackDto>> Handle(UpdateTrackCommand request, CancellationToken cancellationToken)
    {
        var timelineResult = await GetAndAuthorizeAsync(request.TimelineId, cancellationToken);
        if (!timelineResult.IsSuccess) return Result<TrackDto>.Failure(timelineResult.Error);

        var timeline = timelineResult.Value;
        var expectedVersion = timeline.Version;

        timeline.UpdateTrackProperties(request.TrackId, request.Name, request.Locked, request.Muted, request.Hidden, _currentUser.UserId);

        var updated = await _timelineRepository.UpdateAsync(timeline, expectedVersion, cancellationToken);
        if (!updated)
            return Result<TrackDto>.Failure(TimelineErrors.VersionConflict);

        var track = timeline.Tracks.FirstOrDefault(t => t.Id == request.TrackId);
        return Result<TrackDto>.Success(_mapper.Map<TrackDto>(track));
    }
}
