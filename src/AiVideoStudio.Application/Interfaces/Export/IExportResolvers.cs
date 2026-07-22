using AiVideoStudio.Domain.Entities;

namespace AiVideoStudio.Application.Interfaces.Export;

public interface ITimelineResolver
{
    Task<Timeline> ResolveAsync(string timelineId, CancellationToken cancellationToken = default);
}

public interface ITrackResolver
{
    Task<IReadOnlyList<Track>> ResolveAsync(Timeline timeline, CancellationToken cancellationToken = default);
}

public interface IClipResolver
{
    Task<IReadOnlyList<Clip>> ResolveAsync(IReadOnlyList<Track> tracks, CancellationToken cancellationToken = default);
}

public interface IAssetResolver
{
    Task<IReadOnlyDictionary<string, MediaAsset>> ResolveAsync(IReadOnlyList<Clip> clips, CancellationToken cancellationToken = default);
}
