using System;
using System.Collections.Generic;
using System.Linq;
using AiVideoStudio.Domain.Base;
using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Domain.Entities;

public class Timeline : BaseEntity
{
    public string ProjectId { get; private set; } = string.Empty;
    public string OwnerId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public double FrameRate { get; private set; }
    public int ResolutionWidth { get; private set; }
    public int ResolutionHeight { get; private set; }
    
    public bool IsDeleted => DeletedAt.HasValue;

    // Timeline.Duration is automatically computed from the clips
    public TimeSpan Duration
    {
        get
        {
            if (_tracks.Count == 0) return TimeSpan.Zero;
            var allClips = _tracks.SelectMany(t => t.Clips).ToList();
            if (allClips.Count == 0) return TimeSpan.Zero;
            return allClips.Max(c => c.EndFrame);
        }
    }

    private readonly List<Track> _tracks = new();
    public IReadOnlyCollection<Track> Tracks => _tracks.AsReadOnly();

    protected Timeline() 
    {
    }

    public static Timeline Create(
        string projectId, 
        string ownerId, 
        string name, 
        double frameRate = 30.0, 
        int width = 1920, 
        int height = 1080)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Timeline name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("Project ID cannot be empty.", nameof(projectId));
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("Owner ID cannot be empty.", nameof(ownerId));

        var timeline = new Timeline
        {
            ProjectId = projectId,
            OwnerId = ownerId,
            Name = name.Trim(),
            Version = 1,
            FrameRate = frameRate,
            ResolutionWidth = width,
            ResolutionHeight = height,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = ownerId
        };

        return timeline;
    }

    private void IncrementVersion(string updatedBy)
    {
        Version++;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
    
    public void ForceIncrementVersionForAutoSave(string updatedBy)
    {
        IncrementVersion(updatedBy);
    }

    public void Rename(string name, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Timeline name cannot be empty.", nameof(name));

        Name = name.Trim();
        IncrementVersion(updatedBy);
    }

    public void UpdateSettings(double frameRate, int width, int height, string updatedBy)
    {
        FrameRate = frameRate;
        ResolutionWidth = width;
        ResolutionHeight = height;
        IncrementVersion(updatedBy);
    }

    public void SoftDelete(string deletedBy)
    {
        if (IsDeleted) return;

        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
        IncrementVersion(deletedBy);
    }

    public Track AddTrack(string name, TrackType trackType, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Track name cannot be empty.", nameof(name));

        var order = _tracks.Count > 0 ? _tracks.Max(t => t.Order) + 1 : 0;
        var track = new Track(Id, name, order, trackType);
        _tracks.Add(track);
        
        NormalizeTrackOrder();
        IncrementVersion(updatedBy);
        
        return track;
    }

    public void RemoveTrack(string trackId, string updatedBy)
    {
        var track = _tracks.FirstOrDefault(t => t.Id == trackId);
        if (track != null)
        {
            if (track.Clips.Any())
            {
                throw new InvalidOperationException("TrackContainsClips");
            }

            _tracks.Remove(track);
            NormalizeTrackOrder();
            IncrementVersion(updatedBy);
        }
    }

    public void ReorderTrack(string trackId, int newOrder, string updatedBy)
    {
        var track = _tracks.FirstOrDefault(t => t.Id == trackId);
        if (track == null) return;

        if (newOrder < 0) newOrder = 0;
        if (newOrder >= _tracks.Count) newOrder = _tracks.Count - 1;

        var currentOrder = track.Order;
        if (currentOrder == newOrder) return;

        if (newOrder > currentOrder)
        {
            foreach (var t in _tracks.Where(x => x.Order > currentOrder && x.Order <= newOrder))
            {
                t.SetOrder(t.Order - 1);
            }
        }
        else
        {
            foreach (var t in _tracks.Where(x => x.Order >= newOrder && x.Order < currentOrder))
            {
                t.SetOrder(t.Order + 1);
            }
        }

        track.SetOrder(newOrder);
        NormalizeTrackOrder();
        
        IncrementVersion(updatedBy);
    }
    
    public void UpdateTrackProperties(string trackId, string name, bool locked, bool muted, bool hidden, string updatedBy)
    {
        var track = _tracks.FirstOrDefault(t => t.Id == trackId);
        if (track != null)
        {
            track.SetProperties(name, locked, muted, hidden);
            IncrementVersion(updatedBy);
        }
    }

    private void NormalizeTrackOrder()
    {
        var orderedTracks = _tracks.OrderBy(t => t.Order).ToList();
        for (int i = 0; i < orderedTracks.Count; i++)
        {
            orderedTracks[i].SetOrder(i);
        }
    }

    public Clip AddClip(
        string trackId, 
        string assetId, 
        TimeSpan startFrame, 
        TimeSpan endFrame, 
        string name, 
        string? scriptSceneId,
        string? metadata,
        string updatedBy)
    {
        var track = _tracks.FirstOrDefault(t => t.Id == trackId);
        if (track == null)
            throw new InvalidOperationException("Track not found.");

        var clip = track.AddClip(assetId, startFrame, endFrame, name, scriptSceneId, metadata);
        IncrementVersion(updatedBy);
        return clip;
    }

    public void RemoveClip(string clipId, string updatedBy)
    {
        foreach (var track in _tracks)
        {
            var clip = track.Clips.FirstOrDefault(c => c.Id == clipId);
            if (clip != null)
            {
                track.RemoveClip(clipId);
                IncrementVersion(updatedBy);
                return;
            }
        }
    }

    public void ResizeClip(string clipId, TimeSpan newStartFrame, TimeSpan newEndFrame, string updatedBy)
    {
        foreach (var track in _tracks)
        {
            var clip = track.Clips.FirstOrDefault(c => c.Id == clipId);
            if (clip != null)
            {
                track.ResizeClip(clipId, newStartFrame, newEndFrame);
                IncrementVersion(updatedBy);
                return;
            }
        }
    }

    public void MoveClip(string clipId, string newTrackId, TimeSpan newStartFrame, TimeSpan newEndFrame, string updatedBy)
    {
        Track? sourceTrack = null;
        Clip? clipToMove = null;

        foreach (var track in _tracks)
        {
            var c = track.Clips.FirstOrDefault(x => x.Id == clipId);
            if (c != null)
            {
                sourceTrack = track;
                clipToMove = c;
                break;
            }
        }

        if (sourceTrack == null || clipToMove == null)
            throw new InvalidOperationException("Clip not found.");

        if (sourceTrack.Id == newTrackId)
        {
            // Moving within the same track is essentially a resize (but Start and End change)
            sourceTrack.ResizeClip(clipId, newStartFrame, newEndFrame);
        }
        else
        {
            var targetTrack = _tracks.FirstOrDefault(t => t.Id == newTrackId);
            if (targetTrack == null)
                throw new InvalidOperationException("Target track not found.");

            // To avoid leaving clip in a weird state if accept fails due to overlap
            targetTrack.AcceptMovedClip(clipToMove, newStartFrame, newEndFrame);
            sourceTrack.RemoveMovedClip(clipToMove);
        }

        IncrementVersion(updatedBy);
    }
    
    public void UpdateClip(
        string clipId,
        string name,
        int layer,
        double speed,
        TimeSpan trimStart,
        TimeSpan trimEnd,
        double volume,
        string? metadata,
        string updatedBy)
    {
        foreach (var track in _tracks)
        {
            var clip = track.Clips.FirstOrDefault(c => c.Id == clipId);
            if (clip != null)
            {
                track.UpdateClip(clipId, name, layer, speed, trimStart, trimEnd, volume, metadata);
                IncrementVersion(updatedBy);
                return;
            }
        }
    }
}
