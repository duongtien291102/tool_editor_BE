using AiVideoStudio.Application.Interfaces.Export;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;

namespace AiVideoStudio.Infrastructure.Export;

public sealed class TimelineResolver : ITimelineResolver
{
    private readonly ITimelineRepository _repository;
    public TimelineResolver(ITimelineRepository repository) => _repository = repository;

    public async Task<Timeline> ResolveAsync(string timelineId, CancellationToken cancellationToken = default) =>
        await _repository.GetByIdAsync(timelineId, cancellationToken)
        ?? throw new InvalidOperationException($"Timeline '{timelineId}' was not found.");
}

public sealed class TrackResolver : ITrackResolver
{
    public Task<IReadOnlyList<Track>> ResolveAsync(Timeline timeline, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<Track>>(timeline.Tracks.OrderBy(track => track.Order).ToList());
    }
}

public sealed class ClipResolver : IClipResolver
{
    public Task<IReadOnlyList<Clip>> ResolveAsync(IReadOnlyList<Track> tracks, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<Clip>>(
            tracks.SelectMany(track => track.Clips).OrderBy(clip => clip.StartFrame).ToList());
    }
}

public sealed class AssetResolver : IAssetResolver
{
    private readonly IMediaAssetRepository _repository;
    public AssetResolver(IMediaAssetRepository repository) => _repository = repository;

    public async Task<IReadOnlyDictionary<string, MediaAsset>> ResolveAsync(
        IReadOnlyList<Clip> clips,
        CancellationToken cancellationToken = default)
    {
        var assets = new Dictionary<string, MediaAsset>(StringComparer.Ordinal);
        foreach (var assetId in clips.Select(clip => clip.AssetId).Distinct(StringComparer.Ordinal))
        {
            var asset = await _repository.GetByIdAsync(assetId, cancellationToken)
                ?? throw new InvalidOperationException($"Asset '{assetId}' was not found.");
            assets.Add(assetId, asset);
        }

        return assets;
    }
}
