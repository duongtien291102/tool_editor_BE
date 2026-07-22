using System;
using System.Collections.Generic;
using System.Linq;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.DomainErrors; // We will create TimelineErrors

namespace AiVideoStudio.Domain.Entities;

public class Track
{
    public string Id { get; private set; }
    public string TimelineId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public TrackType TrackType { get; private set; }
    public bool Locked { get; private set; }
    public bool Muted { get; private set; }
    public bool Hidden { get; private set; }
    
    private readonly List<Clip> _clips = new();
    public IReadOnlyCollection<Clip> Clips => _clips.AsReadOnly();

    internal Track(string timelineId, string name, int order, TrackType trackType)
    {
        Id = Guid.NewGuid().ToString();
        TimelineId = timelineId;
        Name = name;
        Order = order;
        TrackType = trackType;
        Locked = false;
        Muted = false;
        Hidden = false;
    }

    internal void SetOrder(int newOrder)
    {
        Order = newOrder;
    }
    
    internal void SetProperties(string name, bool locked, bool muted, bool hidden)
    {
        Name = name;
        Locked = locked;
        Muted = muted;
        Hidden = hidden;
    }

    internal Clip AddClip(
        string assetId, 
        TimeSpan startFrame, 
        TimeSpan endFrame, 
        string name = "", 
        string? scriptSceneId = null,
        string? metadata = null)
    {
        if (Locked)
            throw new InvalidOperationException("Cannot add clip to a locked track.");

        CheckOverlap(startFrame, endFrame, null);
        
        var clip = new Clip(TimelineId, Id, assetId, startFrame, endFrame, name, scriptSceneId, metadata);
        _clips.Add(clip);
        return clip;
    }

    internal void RemoveClip(string clipId)
    {
        if (Locked)
            throw new InvalidOperationException("Cannot remove clip from a locked track.");

        var clip = _clips.FirstOrDefault(c => c.Id == clipId);
        if (clip != null)
        {
            _clips.Remove(clip);
        }
    }

    internal void ResizeClip(string clipId, TimeSpan newStartFrame, TimeSpan newEndFrame)
    {
        if (Locked)
            throw new InvalidOperationException("Cannot resize clip on a locked track.");

        var clip = _clips.FirstOrDefault(c => c.Id == clipId);
        if (clip == null)
            throw new InvalidOperationException("Clip not found.");

        CheckOverlap(newStartFrame, newEndFrame, clipId);
        
        clip.Resize(newStartFrame, newEndFrame);
    }
    
    internal void UpdateClip(
        string clipId,
        string name,
        int layer,
        double speed,
        TimeSpan trimStart,
        TimeSpan trimEnd,
        double volume,
        string? metadata)
    {
        if (Locked)
            throw new InvalidOperationException("Cannot update clip on a locked track.");

        var clip = _clips.FirstOrDefault(c => c.Id == clipId);
        if (clip == null)
            throw new InvalidOperationException("Clip not found.");

        clip.Update(name, layer, speed, trimStart, trimEnd, volume, metadata);
    }

    internal void AcceptMovedClip(Clip clip, TimeSpan newStartFrame, TimeSpan newEndFrame)
    {
        if (Locked)
            throw new InvalidOperationException("Cannot move clip to a locked track.");

        CheckOverlap(newStartFrame, newEndFrame, clip.Id);
        
        clip.Move(newStartFrame, newEndFrame, Id);
        _clips.Add(clip);
    }

    internal void RemoveMovedClip(Clip clip)
    {
        if (Locked)
            throw new InvalidOperationException("Cannot move clip from a locked track.");

        _clips.Remove(clip);
    }
    
    private void CheckOverlap(TimeSpan start, TimeSpan end, string? excludeClipId)
    {
        // 1. Clip Overlap Rule: 
        // Video and Audio tracks DO NOT allow overlap.
        // Overlay, Effect, and Subtitle tracks ALLOW overlap.
        if (TrackType != TrackType.Video && TrackType != TrackType.Audio)
        {
            return;
        }

        var hasOverlap = _clips.Any(c => 
            (excludeClipId == null || c.Id != excludeClipId) &&
            // Overlap logic: Start1 < End2 && End1 > Start2
            start < c.EndFrame && end > c.StartFrame
        );

        if (hasOverlap)
        {
            // Note: we can't throw TimelineErrors.ClipOverlap directly if it's just a string, 
            // but we can throw a standard exception that the handler catches, or return a Result.
            // Domain entities throwing Exception is fine if handled by pipeline/middleware,
            // but since we want specific Result.Failure, let's just throw an exception we can catch or 
            // ideally return a domain Result pattern (which the existing codebase uses).
            // Looking at Script, it throws ArgumentException or InvalidOperationException.
            // I'll throw a specific exception to catch later, or InvalidOperationException.
            throw new InvalidOperationException("ClipOverlap"); 
        }
    }
}
