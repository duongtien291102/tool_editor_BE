using System;

namespace AiVideoStudio.Domain.Entities;

public class Clip
{
    public string Id { get; private set; }
    public string TimelineId { get; private set; }
    public string TrackId { get; private set; }
    public string AssetId { get; private set; }
    public string? ScriptSceneId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public TimeSpan StartFrame { get; private set; }
    public TimeSpan EndFrame { get; private set; }
    
    // Duration is always synchronized and calculated
    public TimeSpan Duration => EndFrame - StartFrame;
    
    public int Layer { get; private set; }
    public double Speed { get; private set; }
    public TimeSpan TrimStart { get; private set; }
    public TimeSpan TrimEnd { get; private set; }
    public double Volume { get; private set; }
    public string? Metadata { get; private set; }

    internal Clip(
        string timelineId, 
        string trackId, 
        string assetId, 
        TimeSpan startFrame, 
        TimeSpan endFrame, 
        string name = "", 
        string? scriptSceneId = null,
        string? metadata = null)
    {
        if (startFrame < TimeSpan.Zero)
            throw new ArgumentException("StartFrame must be >= 0.", nameof(startFrame));
        if (endFrame <= startFrame)
            throw new ArgumentException("EndFrame must be strictly greater than StartFrame.", nameof(endFrame));
        
        Id = Guid.NewGuid().ToString();
        TimelineId = timelineId;
        TrackId = trackId;
        AssetId = assetId;
        StartFrame = startFrame;
        EndFrame = endFrame;
        Name = name;
        ScriptSceneId = scriptSceneId;
        
        // Defaults
        Layer = 1;
        Speed = 1.0;
        TrimStart = TimeSpan.Zero;
        TrimEnd = TimeSpan.Zero;
        Volume = 1.0;
        Metadata = metadata;
    }

    internal void Move(TimeSpan newStartFrame, TimeSpan newEndFrame, string newTrackId)
    {
        if (newStartFrame < TimeSpan.Zero)
            throw new ArgumentException("StartFrame must be >= 0.", nameof(newStartFrame));
        if (newEndFrame <= newStartFrame)
            throw new ArgumentException("EndFrame must be strictly greater than StartFrame.", nameof(newEndFrame));
        
        StartFrame = newStartFrame;
        EndFrame = newEndFrame;
        TrackId = newTrackId;
    }

    internal void Resize(TimeSpan newStartFrame, TimeSpan newEndFrame)
    {
        if (newStartFrame < TimeSpan.Zero)
            throw new ArgumentException("StartFrame must be >= 0.", nameof(newStartFrame));
        if (newEndFrame <= newStartFrame)
            throw new ArgumentException("EndFrame must be strictly greater than StartFrame.", nameof(newEndFrame));
            
        StartFrame = newStartFrame;
        EndFrame = newEndFrame;
    }
    
    internal void Update(
        string name,
        int layer,
        double speed,
        TimeSpan trimStart,
        TimeSpan trimEnd,
        double volume,
        string? metadata)
    {
        Name = name;
        Layer = layer;
        Speed = speed;
        TrimStart = trimStart;
        TrimEnd = trimEnd;
        Volume = volume;
        Metadata = metadata;
    }
}
