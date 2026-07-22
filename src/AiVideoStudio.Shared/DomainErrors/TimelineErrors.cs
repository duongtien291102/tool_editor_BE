using AiVideoStudio.Shared.Responses;

namespace AiVideoStudio.Shared.DomainErrors;

public static class TimelineErrors
{
    public static readonly Error NotFound = new(
        "Timeline.NotFound",
        "The timeline was not found.");

    public static readonly Error AlreadyExists = new(
        "Timeline.AlreadyExists",
        "A timeline already exists for this project.");

    public static readonly Error VersionConflict = new(
        "Timeline.VersionConflict",
        "The timeline was modified by another process. Version conflict.");

    public static readonly Error TrackContainsClips = new(
        "Timeline.TrackContainsClips",
        "Cannot delete a track that contains clips. Please delete or move all clips first.");

    public static readonly Error ClipOverlap = new(
        "Timeline.ClipOverlap",
        "Clips cannot overlap on this track type.");
}
