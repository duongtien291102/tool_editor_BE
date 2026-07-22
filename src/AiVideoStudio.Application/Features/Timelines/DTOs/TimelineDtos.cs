using System;
using System.Collections.Generic;
using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Application.Features.Timelines.DTOs;

public record TimelineDto(
    string Id,
    string ProjectId,
    string OwnerId,
    string Name,
    int Version,
    double FrameRate,
    int ResolutionWidth,
    int ResolutionHeight,
    TimeSpan Duration,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    List<TrackDto> Tracks
);

public record TrackDto(
    string Id,
    string Name,
    int Order,
    TrackType TrackType,
    bool Locked,
    bool Muted,
    bool Hidden,
    List<ClipDto> Clips
);

public record ClipDto(
    string Id,
    string TrackId,
    string AssetId,
    string? ScriptSceneId,
    string Name,
    TimeSpan StartFrame,
    TimeSpan EndFrame,
    TimeSpan Duration,
    int Layer,
    double Speed,
    TimeSpan TrimStart,
    TimeSpan TrimEnd,
    double Volume,
    string? Metadata
);
