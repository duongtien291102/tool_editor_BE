using AiVideoStudio.Application.Features.Exports.Models;
using AiVideoStudio.Application.Interfaces.Export;
using AiVideoStudio.Domain.Entities;

namespace AiVideoStudio.Infrastructure.Export;

public sealed class ExportGraphBuilder : IExportGraphBuilder
{
    public RenderGraph Build(
        Timeline timeline,
        IReadOnlyList<Track> tracks,
        IReadOnlyList<Clip> clips,
        IReadOnlyDictionary<string, MediaAsset> assets)
    {
        ArgumentNullException.ThrowIfNull(timeline);

        var nodes = clips.Select(clip =>
        {
            var track = tracks.Single(item => item.Id == clip.TrackId);
            var asset = assets.TryGetValue(clip.AssetId, out var resolved)
                ? resolved
                : throw new InvalidOperationException($"Asset '{clip.AssetId}' was not resolved.");
            return new RenderGraphNode(
                clip.Id,
                track.Id,
                clip.AssetId,
                track.TrackType,
                clip.StartFrame,
                clip.Duration,
                clip.Layer,
                asset.StoragePath,
                clip.Metadata);
        }).ToList();

        var layers = tracks.Select(track => new RenderGraphLayer(
            track.Id,
            track.TrackType,
            track.Order,
            nodes.Where(node => node.TrackId == track.Id).OrderBy(node => node.Start).Select(node => node.Id).ToList()))
            .ToList();

        var edges = new List<RenderGraphEdge>();
        foreach (var layer in layers)
        {
            for (var index = 1; index < layer.NodeIds.Count; index++)
                edges.Add(new RenderGraphEdge(layer.NodeIds[index - 1], layer.NodeIds[index], "sequence"));
        }

        var dependencies = nodes
            .Select(node => new RenderGraphDependency(node.Id, node.AssetId, node.SourcePath))
            .ToList();
        var timelineGraph = new TimelineGraph(
            timeline.Id,
            timeline.FrameRate,
            timeline.ResolutionWidth,
            timeline.ResolutionHeight,
            timeline.Duration);

        return new RenderGraph(timelineGraph, nodes, edges, layers, dependencies);
    }
}
