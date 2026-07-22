using AiVideoStudio.Application.Features.Exports.Models;
using AiVideoStudio.Application.Interfaces.Export;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Infrastructure.Export;

public sealed class FFmpegCommandBuilder : IFFmpegCommandBuilder
{
    public FFmpegCommandModel Build(RenderGraph graph, ExportJob job, string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new InvalidOperationException("Export output directory is not configured.");

        var inputs = graph.Nodes.Select(node => new FFmpegInputAsset(
            node.AssetId, node.SourcePath, node.Start, node.Duration)).ToList();

        var video = graph.Nodes.Where(node => node.TrackType is TrackType.Video or TrackType.Effect)
            .Select(node => new VideoFilter(node.Id, "scale-and-timeline", new Dictionary<string, string>
            {
                ["resolution"] = job.Resolution,
                ["start"] = node.Start.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)
            })).ToList();
        var audio = graph.Nodes.Where(node => node.TrackType == TrackType.Audio)
            .Select(node => new AudioFilter(node.Id, 1.0, node.Start)).ToList();
        var subtitles = graph.Nodes.Where(node => node.TrackType == TrackType.Subtitle)
            .Select(node => new SubtitleFilter(node.Id, node.SourcePath, node.Start)).ToList();
        var overlays = graph.Nodes.Where(node => node.TrackType == TrackType.Overlay)
            .Select(node => new OverlayFilter(node.Id, node.Layer, node.Start, node.Duration)).ToList();
        var transitions = graph.Edges
            .Select(edge => new TransitionFilter(edge.FromNodeId, edge.ToNodeId, TimeSpan.FromMilliseconds(250)))
            .ToList();

        var extension = job.Container.ToString().ToLowerInvariant();
        var output = new FFmpegOutputOptions(
            outputDirectory,
            $"{job.Id}.{extension}",
            job.Resolution,
            job.FrameRate,
            job.VideoCodec,
            job.AudioCodec,
            job.Container);
        return new FFmpegCommandModel(inputs, video, audio, subtitles, transitions, overlays, output);
    }
}
