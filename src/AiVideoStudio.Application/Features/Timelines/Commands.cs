using System;
using AiVideoStudio.Application.Features.Timelines.DTOs;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Timelines;

// Timeline Commands
public record CreateTimelineCommand(string ProjectId, string Name, double FrameRate = 30.0, int ResolutionWidth = 1920, int ResolutionHeight = 1080) : IRequest<Result<TimelineDto>>;
public record UpdateTimelineCommand(string Id, string Name, double FrameRate, int ResolutionWidth, int ResolutionHeight) : IRequest<Result<TimelineDto>>;
public record DeleteTimelineCommand(string Id) : IRequest<Result>;
public record AutoSaveTimelineCommand(string Id, TimelineDto Data) : IRequest<Result<TimelineDto>>;

// Track Commands
public record AddTrackCommand(string TimelineId, string Name, TrackType TrackType) : IRequest<Result<TrackDto>>;
public record RemoveTrackCommand(string TimelineId, string TrackId) : IRequest<Result>;
public record ReorderTrackCommand(string TimelineId, string TrackId, int NewOrder) : IRequest<Result>;
public record UpdateTrackCommand(string TimelineId, string TrackId, string Name, bool Locked, bool Muted, bool Hidden) : IRequest<Result<TrackDto>>;

// Clip Commands
public record AddClipCommand(
    string TimelineId, 
    string TrackId, 
    string AssetId, 
    TimeSpan StartFrame, 
    TimeSpan EndFrame, 
    string Name, 
    string? ScriptSceneId, 
    string? Metadata) : IRequest<Result<ClipDto>>;

public record UpdateClipCommand(
    string TimelineId, 
    string ClipId, 
    string Name,
    int Layer,
    double Speed,
    TimeSpan TrimStart,
    TimeSpan TrimEnd,
    double Volume,
    string? Metadata) : IRequest<Result<ClipDto>>;

public record MoveClipCommand(
    string TimelineId, 
    string ClipId, 
    string NewTrackId, 
    TimeSpan NewStartFrame, 
    TimeSpan NewEndFrame) : IRequest<Result<ClipDto>>;

public record ResizeClipCommand(
    string TimelineId, 
    string ClipId, 
    TimeSpan NewStartFrame, 
    TimeSpan NewEndFrame) : IRequest<Result<ClipDto>>;

public record DeleteClipCommand(string TimelineId, string ClipId) : IRequest<Result>;
