using System;
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

public class ClipCommandsHandler : 
    IRequestHandler<AddClipCommand, Result<ClipDto>>,
    IRequestHandler<UpdateClipCommand, Result<ClipDto>>,
    IRequestHandler<MoveClipCommand, Result<ClipDto>>,
    IRequestHandler<ResizeClipCommand, Result<ClipDto>>,
    IRequestHandler<DeleteClipCommand, Result>
{
    private readonly ITimelineRepository _timelineRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;

    public ClipCommandsHandler(
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

    public async Task<Result<ClipDto>> Handle(AddClipCommand request, CancellationToken cancellationToken)
    {
        var timelineResult = await GetAndAuthorizeAsync(request.TimelineId, cancellationToken);
        if (!timelineResult.IsSuccess) return Result<ClipDto>.Failure(timelineResult.Error);

        var timeline = timelineResult.Value;
        var expectedVersion = timeline.Version;

        try
        {
            var clip = timeline.AddClip(
                request.TrackId, 
                request.AssetId, 
                request.StartFrame, 
                request.EndFrame, 
                request.Name, 
                request.ScriptSceneId, 
                request.Metadata, 
                _currentUser.UserId);

            var updated = await _timelineRepository.UpdateAsync(timeline, expectedVersion, cancellationToken);
            if (!updated)
                return Result<ClipDto>.Failure(TimelineErrors.VersionConflict);

            return Result<ClipDto>.Success(_mapper.Map<ClipDto>(clip));
        }
        catch (InvalidOperationException ex) when (ex.Message == "ClipOverlap")
        {
            return Result<ClipDto>.Failure(TimelineErrors.ClipOverlap);
        }
        catch (InvalidOperationException ex)
        {
            return Result<ClipDto>.Failure(new Error("Timeline.InvalidOperation", ex.Message));
        }
    }

    public async Task<Result<ClipDto>> Handle(UpdateClipCommand request, CancellationToken cancellationToken)
    {
        var timelineResult = await GetAndAuthorizeAsync(request.TimelineId, cancellationToken);
        if (!timelineResult.IsSuccess) return Result<ClipDto>.Failure(timelineResult.Error);

        var timeline = timelineResult.Value;
        var expectedVersion = timeline.Version;

        try
        {
            timeline.UpdateClip(
                request.ClipId, 
                request.Name, 
                request.Layer, 
                request.Speed, 
                request.TrimStart, 
                request.TrimEnd, 
                request.Volume, 
                request.Metadata, 
                _currentUser.UserId);

            var updated = await _timelineRepository.UpdateAsync(timeline, expectedVersion, cancellationToken);
            if (!updated)
                return Result<ClipDto>.Failure(TimelineErrors.VersionConflict);

            var clip = timeline.Tracks.SelectMany(t => t.Clips).FirstOrDefault(c => c.Id == request.ClipId);
            return Result<ClipDto>.Success(_mapper.Map<ClipDto>(clip));
        }
        catch (InvalidOperationException ex)
        {
            return Result<ClipDto>.Failure(new Error("Timeline.InvalidOperation", ex.Message));
        }
    }

    public async Task<Result<ClipDto>> Handle(MoveClipCommand request, CancellationToken cancellationToken)
    {
        var timelineResult = await GetAndAuthorizeAsync(request.TimelineId, cancellationToken);
        if (!timelineResult.IsSuccess) return Result<ClipDto>.Failure(timelineResult.Error);

        var timeline = timelineResult.Value;
        var expectedVersion = timeline.Version;

        try
        {
            timeline.MoveClip(request.ClipId, request.NewTrackId, request.NewStartFrame, request.NewEndFrame, _currentUser.UserId);

            var updated = await _timelineRepository.UpdateAsync(timeline, expectedVersion, cancellationToken);
            if (!updated)
                return Result<ClipDto>.Failure(TimelineErrors.VersionConflict);

            var clip = timeline.Tracks.SelectMany(t => t.Clips).FirstOrDefault(c => c.Id == request.ClipId);
            return Result<ClipDto>.Success(_mapper.Map<ClipDto>(clip));
        }
        catch (InvalidOperationException ex) when (ex.Message == "ClipOverlap")
        {
            return Result<ClipDto>.Failure(TimelineErrors.ClipOverlap);
        }
        catch (InvalidOperationException ex)
        {
            return Result<ClipDto>.Failure(new Error("Timeline.InvalidOperation", ex.Message));
        }
    }

    public async Task<Result<ClipDto>> Handle(ResizeClipCommand request, CancellationToken cancellationToken)
    {
        var timelineResult = await GetAndAuthorizeAsync(request.TimelineId, cancellationToken);
        if (!timelineResult.IsSuccess) return Result<ClipDto>.Failure(timelineResult.Error);

        var timeline = timelineResult.Value;
        var expectedVersion = timeline.Version;

        try
        {
            timeline.ResizeClip(request.ClipId, request.NewStartFrame, request.NewEndFrame, _currentUser.UserId);

            var updated = await _timelineRepository.UpdateAsync(timeline, expectedVersion, cancellationToken);
            if (!updated)
                return Result<ClipDto>.Failure(TimelineErrors.VersionConflict);

            var clip = timeline.Tracks.SelectMany(t => t.Clips).FirstOrDefault(c => c.Id == request.ClipId);
            return Result<ClipDto>.Success(_mapper.Map<ClipDto>(clip));
        }
        catch (InvalidOperationException ex) when (ex.Message == "ClipOverlap")
        {
            return Result<ClipDto>.Failure(TimelineErrors.ClipOverlap);
        }
        catch (InvalidOperationException ex)
        {
            return Result<ClipDto>.Failure(new Error("Timeline.InvalidOperation", ex.Message));
        }
    }

    public async Task<Result> Handle(DeleteClipCommand request, CancellationToken cancellationToken)
    {
        var timelineResult = await GetAndAuthorizeAsync(request.TimelineId, cancellationToken);
        if (!timelineResult.IsSuccess) return Result.Failure(timelineResult.Error);

        var timeline = timelineResult.Value;
        var expectedVersion = timeline.Version;

        try
        {
            timeline.RemoveClip(request.ClipId, _currentUser.UserId);

            var updated = await _timelineRepository.UpdateAsync(timeline, expectedVersion, cancellationToken);
            if (!updated)
                return Result.Failure(TimelineErrors.VersionConflict);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(new Error("Timeline.InvalidOperation", ex.Message));
        }
    }
}
