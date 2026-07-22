using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Application.Features.Exports.Models;

public sealed record TimelineGraph(
    string TimelineId,
    double FrameRate,
    int Width,
    int Height,
    TimeSpan Duration);

public sealed record RenderGraphNode(
    string Id,
    string TrackId,
    string AssetId,
    TrackType TrackType,
    TimeSpan Start,
    TimeSpan Duration,
    int Layer,
    string SourcePath,
    string? Metadata);

public sealed record RenderGraphEdge(string FromNodeId, string ToNodeId, string Relation);
public sealed record RenderGraphLayer(string TrackId, TrackType TrackType, int Order, IReadOnlyList<string> NodeIds);
public sealed record RenderGraphDependency(string NodeId, string AssetId, string SourcePath);

public sealed record RenderGraph(
    TimelineGraph Timeline,
    IReadOnlyList<RenderGraphNode> Nodes,
    IReadOnlyList<RenderGraphEdge> Edges,
    IReadOnlyList<RenderGraphLayer> Layers,
    IReadOnlyList<RenderGraphDependency> Dependencies);
