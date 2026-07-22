using System;
using AiVideoStudio.Application.Features.Timelines.DTOs;
using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Api.Contracts.V1.Timelines.Requests;

public class CreateTimelineRequest
{
    public string ProjectId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double FrameRate { get; set; } = 30.0;
    public int ResolutionWidth { get; set; } = 1920;
    public int ResolutionHeight { get; set; } = 1080;
}

public class UpdateTimelineRequest
{
    public string Name { get; set; } = string.Empty;
    public double FrameRate { get; set; }
    public int ResolutionWidth { get; set; }
    public int ResolutionHeight { get; set; }
}

public class AutoSaveTimelineRequest
{
    public TimelineDto Data { get; set; } = default!;
}

public class AddTrackRequest
{
    public string Name { get; set; } = string.Empty;
    public TrackType TrackType { get; set; }
}

public class ReorderTrackRequest
{
    public int NewOrder { get; set; }
}

public class AddClipRequest
{
    public string TrackId { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public TimeSpan StartFrame { get; set; }
    public TimeSpan EndFrame { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ScriptSceneId { get; set; }
    public string? Metadata { get; set; }
}

public class UpdateClipRequest
{
    public string Name { get; set; } = string.Empty;
    public int Layer { get; set; }
    public double Speed { get; set; } = 1.0;
    public TimeSpan TrimStart { get; set; }
    public TimeSpan TrimEnd { get; set; }
    public double Volume { get; set; } = 1.0;
    public string? Metadata { get; set; }
}

public class MoveClipRequest
{
    public string NewTrackId { get; set; } = string.Empty;
    public TimeSpan NewStartFrame { get; set; }
    public TimeSpan NewEndFrame { get; set; }
}

public class ResizeClipRequest
{
    public TimeSpan NewStartFrame { get; set; }
    public TimeSpan NewEndFrame { get; set; }
}
