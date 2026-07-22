using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Features.Exports.Models;
using AiVideoStudio.Application.Interfaces.Export;
using AiVideoStudio.Domain.Entities;
using Microsoft.Extensions.Options;

namespace AiVideoStudio.Infrastructure.Export;

public sealed class ExportPipeline : IExportPipeline
{
    private readonly ITimelineResolver _timelineResolver;
    private readonly ITrackResolver _trackResolver;
    private readonly IClipResolver _clipResolver;
    private readonly IAssetResolver _assetResolver;
    private readonly IExportGraphBuilder _graphBuilder;
    private readonly IFFmpegCommandBuilder _commandBuilder;
    private readonly IExportProvider _provider;
    private readonly ExportOptions _options;

    public ExportPipeline(
        ITimelineResolver timelineResolver,
        ITrackResolver trackResolver,
        IClipResolver clipResolver,
        IAssetResolver assetResolver,
        IExportGraphBuilder graphBuilder,
        IFFmpegCommandBuilder commandBuilder,
        IExportProvider provider,
        IOptions<ExportOptions> options)
    {
        _timelineResolver = timelineResolver;
        _trackResolver = trackResolver;
        _clipResolver = clipResolver;
        _assetResolver = assetResolver;
        _graphBuilder = graphBuilder;
        _commandBuilder = commandBuilder;
        _provider = provider;
        _options = options.Value;
    }

    public async Task<ExportProviderResult> ExecuteAsync(
        ExportJob job,
        Func<ExportProgressUpdate, CancellationToken, Task> progress,
        CancellationToken cancellationToken = default)
    {
        var timeline = await _timelineResolver.ResolveAsync(job.TimelineId, cancellationToken);
        var tracks = await _trackResolver.ResolveAsync(timeline, cancellationToken);
        var clips = await _clipResolver.ResolveAsync(tracks, cancellationToken);
        var assets = await _assetResolver.ResolveAsync(clips, cancellationToken);
        var graph = _graphBuilder.Build(timeline, tracks, clips, assets);
        var command = _commandBuilder.Build(graph, job, _options.OutputDirectory);
        return await _provider.ExportAsync(command, progress, cancellationToken);
    }
}
