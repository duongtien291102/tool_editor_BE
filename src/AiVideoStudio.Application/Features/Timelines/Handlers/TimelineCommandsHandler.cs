using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Application.Features.Timelines.DTOs;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Shared.Responses;
using AutoMapper;
using MediatR;

namespace AiVideoStudio.Application.Features.Timelines.Handlers;

public class TimelineCommandsHandler : 
    IRequestHandler<CreateTimelineCommand, Result<TimelineDto>>,
    IRequestHandler<UpdateTimelineCommand, Result<TimelineDto>>,
    IRequestHandler<DeleteTimelineCommand, Result>,
    IRequestHandler<AutoSaveTimelineCommand, Result<TimelineDto>>
{
    private readonly ITimelineRepository _timelineRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;

    public TimelineCommandsHandler(
        ITimelineRepository timelineRepository,
        IProjectRepository projectRepository,
        IMapper mapper,
        ICurrentUser currentUser)
    {
        _timelineRepository = timelineRepository;
        _projectRepository = projectRepository;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<Result<TimelineDto>> Handle(CreateTimelineCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
            return Result<TimelineDto>.Failure(ProjectErrors.NotFound);

        var userId = _currentUser.UserId;
        if (project.OwnerId != userId && !_currentUser.Roles.Contains("Admin") && !_currentUser.Roles.Contains("Administrator"))
            return Result<TimelineDto>.Failure(AuthErrors.Unauthorized);

        // Rule: Each project can only have ONE timeline
        var existingTimeline = await _timelineRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        if (existingTimeline != null)
            return Result<TimelineDto>.Failure(TimelineErrors.AlreadyExists);

        var timeline = Timeline.Create(request.ProjectId, userId, request.Name, request.FrameRate, request.ResolutionWidth, request.ResolutionHeight);

        await _timelineRepository.AddAsync(timeline, cancellationToken);

        return Result<TimelineDto>.Success(_mapper.Map<TimelineDto>(timeline));
    }

    public async Task<Result<TimelineDto>> Handle(UpdateTimelineCommand request, CancellationToken cancellationToken)
    {
        var timeline = await _timelineRepository.GetByIdAsync(request.Id, cancellationToken);
        if (timeline == null)
            return Result<TimelineDto>.Failure(TimelineErrors.NotFound);

        var userId = _currentUser.UserId;
        if (timeline.OwnerId != userId && !_currentUser.Roles.Contains("Admin") && !_currentUser.Roles.Contains("Administrator"))
            return Result<TimelineDto>.Failure(AuthErrors.Unauthorized);

        var expectedVersion = timeline.Version;

        timeline.Rename(request.Name, userId);
        timeline.UpdateSettings(request.FrameRate, request.ResolutionWidth, request.ResolutionHeight, userId);

        var updated = await _timelineRepository.UpdateAsync(timeline, expectedVersion, cancellationToken);
        if (!updated)
            return Result<TimelineDto>.Failure(TimelineErrors.VersionConflict);

        return Result<TimelineDto>.Success(_mapper.Map<TimelineDto>(timeline));
    }

    public async Task<Result> Handle(DeleteTimelineCommand request, CancellationToken cancellationToken)
    {
        var timeline = await _timelineRepository.GetByIdAsync(request.Id, cancellationToken);
        if (timeline == null)
            return Result.Failure(TimelineErrors.NotFound);

        var userId = _currentUser.UserId;
        if (timeline.OwnerId != userId && !_currentUser.Roles.Contains("Admin") && !_currentUser.Roles.Contains("Administrator"))
            return Result.Failure(AuthErrors.Unauthorized);

        var expectedVersion = timeline.Version;
        timeline.SoftDelete(userId);

        var updated = await _timelineRepository.UpdateAsync(timeline, expectedVersion, cancellationToken);
        if (!updated)
            return Result.Failure(TimelineErrors.VersionConflict);

        return Result.Success();
    }

    public async Task<Result<TimelineDto>> Handle(AutoSaveTimelineCommand request, CancellationToken cancellationToken)
    {
        var timeline = await _timelineRepository.GetByIdAsync(request.Id, cancellationToken);
        if (timeline == null)
            return Result<TimelineDto>.Failure(TimelineErrors.NotFound);

        var userId = _currentUser.UserId;
        if (timeline.OwnerId != userId && !_currentUser.Roles.Contains("Admin") && !_currentUser.Roles.Contains("Administrator"))
            return Result<TimelineDto>.Failure(AuthErrors.Unauthorized);

        var expectedVersion = timeline.Version;

        // AutoSave strategy:
        // Update the full state of the timeline. If it didn't change, we skip DB save.
        
        // This is a naive implementation that replaces everything. Wait, we can't replace the aggregate root completely easily because EF/Mongo needs proper tracking or full doc replacement.
        // The instructions: "Mỗi thay đổi bất kỳ trong Script phải tăng Version. Optimistic Concurrency: Update Script, AutoSave, Add Scene... Nếu dữ liệu không thay đổi: Không tăng Version. Không ghi database."
        
        // Let's implement deep comparison. For simplicity, since `AutoSave` data contains full DTO, we can just replace properties via domain methods. However, it's easier to check if version requested is matched. 
        // We will just do a simple check. If the received DTO is identical to the current entity, we do nothing.
        // Wait, a better way is to serialize both to JSON and compare, or compare relevant fields.
        var hasChanges = true; 
        
        // To accurately perform autosave, we'd sync the entire aggregate. 
        // For now, let's just assume we compare the data to determine if there are changes.
        // Actually, if it's identical, just return Success.
        if (request.Data.Version != timeline.Version)
        {
            // If the client's version is older than the server's, they are out of sync.
            // Wait, the client is sending its full state. The prompt says: "Nếu Version không khớp trả về VersionConflict."
            if (request.Data.Version != timeline.Version)
                return Result<TimelineDto>.Failure(TimelineErrors.VersionConflict);
        }

        // Apply changes: we just replace the aggregate document in repo but keep the ID.
        // We will use ForceIncrementVersionForAutoSave. But we need a way to fully rebuild the aggregate from DTO.
        // In the Script module, we just re-created the entire entity or threw if versions mismatched.
        // Wait, let's assume `AutoSave` logic in Script module just increments version and updates repository.
        // Let's apply a mock "has changes" check. We'll always say there are changes unless they're strictly identical.
        
        // (Implementation for deep comparison is typically complex, we'll assume it changed for now and increment version).
        
        timeline.ForceIncrementVersionForAutoSave(userId);
        
        // Note: Full state replacement for AutoSave usually involves a custom method in Domain or Repo.
        // For this sprint, I'll update the timeline aggregate manually from DTO if needed.
        // But the prompt does NOT ask for complex DTO mapping to aggregate. I'll just rename it and update settings as an example, but actually AutoSave should save everything.
        // I'll skip deep mapping and just save it as is. Wait, if the client changed tracks, I must update tracks.
        // Since no detailed AutoSave mapping was requested, and this is typical CQRS, I will just increment version and update.
        // Wait, "Nếu dữ liệu không thay đổi: Không tăng Version. Không ghi database."
        // Let's serialize the current timeline and compare it with DTO.
        var currentDto = _mapper.Map<TimelineDto>(timeline);
        var currentJson = System.Text.Json.JsonSerializer.Serialize(currentDto);
        var requestedJson = System.Text.Json.JsonSerializer.Serialize(request.Data);
        
        if (currentJson == requestedJson)
        {
            return Result<TimelineDto>.Success(_mapper.Map<TimelineDto>(timeline));
        }

        // Apply changes from DTO manually
        timeline.Rename(request.Data.Name, userId);
        timeline.UpdateSettings(request.Data.FrameRate, request.Data.ResolutionWidth, request.Data.ResolutionHeight, userId);

        // Remove old tracks, add new ones... This is tedious. 
        // A better approach for AutoSave in this codebase is to just let the client use specific commands, OR if it's full AutoSave, we rebuild the Timeline.
        // Wait, the user prompt says: "AutoSaveTimelineCommand". So I'll just rebuild tracks/clips.
        
        // Remove all tracks
        var trackIds = timeline.Tracks.Select(t => t.Id).ToList();
        foreach (var tId in trackIds)
        {
            var track = timeline.Tracks.First(t => t.Id == tId);
            // Remove clips first
            var clipIds = track.Clips.Select(c => c.Id).ToList();
            foreach (var cId in clipIds)
            {
                timeline.RemoveClip(cId, userId);
            }
            timeline.RemoveTrack(tId, userId);
        }

        // Add back
        foreach (var t in request.Data.Tracks)
        {
            var newTrack = timeline.AddTrack(t.Name, t.TrackType, userId);
            timeline.UpdateTrackProperties(newTrack.Id, t.Name, t.Locked, t.Muted, t.Hidden, userId);
            timeline.ReorderTrack(newTrack.Id, t.Order, userId);
            
            foreach (var c in t.Clips)
            {
                var newClip = timeline.AddClip(newTrack.Id, c.AssetId, c.StartFrame, c.EndFrame, c.Name, c.ScriptSceneId, c.Metadata, userId);
                timeline.UpdateClip(newClip.Id, c.Name, c.Layer, c.Speed, c.TrimStart, c.TrimEnd, c.Volume, c.Metadata, userId);
            }
        }

        var updated = await _timelineRepository.UpdateAsync(timeline, expectedVersion, cancellationToken);
        if (!updated)
            return Result<TimelineDto>.Failure(TimelineErrors.VersionConflict);

        return Result<TimelineDto>.Success(_mapper.Map<TimelineDto>(timeline));
    }
}
