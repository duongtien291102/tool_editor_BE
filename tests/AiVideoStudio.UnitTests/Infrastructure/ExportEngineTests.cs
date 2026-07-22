using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Features.Exports.Models;
using AiVideoStudio.Application.Interfaces.Export;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Infrastructure.Export;
using AiVideoStudio.Infrastructure.Mongo.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace AiVideoStudio.UnitTests.Infrastructure;

public class ExportEngineTests
{
    [Fact]
    public async Task Resolvers_ShouldResolveTimelineTracksClipsAndAssets()
    {
        var timeline = BuildTimeline(out var asset);
        var timelines = Substitute.For<ITimelineRepository>();
        var media = Substitute.For<IMediaAssetRepository>();
        timelines.GetByIdAsync(timeline.Id, Arg.Any<CancellationToken>()).Returns(timeline);
        media.GetByIdAsync(asset.Id, Arg.Any<CancellationToken>()).Returns(asset);

        var resolvedTimeline = await new TimelineResolver(timelines).ResolveAsync(timeline.Id);
        var tracks = await new TrackResolver().ResolveAsync(resolvedTimeline);
        var clips = await new ClipResolver().ResolveAsync(tracks);
        var assets = await new AssetResolver(media).ResolveAsync(clips);

        tracks.Should().HaveCount(2);
        clips.Should().HaveCount(2);
        assets.Should().ContainKey(asset.Id);
    }

    [Fact]
    public void GraphBuilder_ShouldCreateNodesLayersEdgesAndDependencies()
    {
        var timeline = BuildTimeline(out var asset);
        var tracks = timeline.Tracks.OrderBy(item => item.Order).ToList();
        var clips = tracks.SelectMany(item => item.Clips).ToList();

        var graph = new ExportGraphBuilder().Build(timeline, tracks, clips,
            new Dictionary<string, MediaAsset> { [asset.Id] = asset });

        graph.Nodes.Should().HaveCount(2);
        graph.Layers.Should().HaveCount(2);
        graph.Dependencies.Should().HaveCount(2);
        graph.Timeline.Width.Should().Be(1920);
    }

    [Fact]
    public void CommandBuilder_ShouldBuildTypedModelWithoutExecutableCommand()
    {
        var job = ExportJob.Create("r", "p", "t", "owner", TimeSpan.FromSeconds(2), "1920x1080", 30,
            VideoCodec.H264, AudioCodec.AAC, ContainerFormat.MP4);
        var graph = new RenderGraph(
            new TimelineGraph("t", 30, 1920, 1080, TimeSpan.FromSeconds(2)),
            new[] { new RenderGraphNode("n", "track", "asset", TrackType.Video, TimeSpan.Zero, TimeSpan.FromSeconds(2), 1, "source.mp4", null) },
            Array.Empty<RenderGraphEdge>(),
            new[] { new RenderGraphLayer("track", TrackType.Video, 0, new[] { "n" }) },
            new[] { new RenderGraphDependency("n", "asset", "source.mp4") });

        var command = new FFmpegCommandBuilder().Build(graph, job, "configured-output");

        command.InputAssets.Should().ContainSingle();
        command.VideoFilters.Should().ContainSingle();
        command.OutputOptions.FileName.Should().EndWith(".mp4");
        command.OutputOptions.OutputDirectory.Should().Be("configured-output");
    }

    [Fact]
    public async Task Pipeline_ShouldExecuteEveryStageAndProvider()
    {
        var job = ExportJob.Create("r", "p", "t", "owner", TimeSpan.Zero, "1920x1080", 30,
            VideoCodec.H264, AudioCodec.AAC, ContainerFormat.MP4);
        var timeline = Timeline.Create("p", "owner", "timeline");
        var timelineResolver = Substitute.For<ITimelineResolver>();
        var trackResolver = Substitute.For<ITrackResolver>();
        var clipResolver = Substitute.For<IClipResolver>();
        var assetResolver = Substitute.For<IAssetResolver>();
        var graphBuilder = Substitute.For<IExportGraphBuilder>();
        var commandBuilder = Substitute.For<IFFmpegCommandBuilder>();
        var provider = Substitute.For<IExportProvider>();
        var graph = new RenderGraph(new TimelineGraph("t", 30, 1920, 1080, TimeSpan.Zero),
            Array.Empty<RenderGraphNode>(), Array.Empty<RenderGraphEdge>(), Array.Empty<RenderGraphLayer>(), Array.Empty<RenderGraphDependency>());
        var command = EmptyCommand("output");
        timelineResolver.ResolveAsync("t", Arg.Any<CancellationToken>()).Returns(timeline);
        trackResolver.ResolveAsync(timeline, Arg.Any<CancellationToken>()).Returns(Array.Empty<Track>());
        clipResolver.ResolveAsync(Arg.Any<IReadOnlyList<Track>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<Clip>());
        assetResolver.ResolveAsync(Arg.Any<IReadOnlyList<Clip>>(), Arg.Any<CancellationToken>()).Returns(new Dictionary<string, MediaAsset>());
        graphBuilder.Build(timeline, Arg.Any<IReadOnlyList<Track>>(), Arg.Any<IReadOnlyList<Clip>>(), Arg.Any<IReadOnlyDictionary<string, MediaAsset>>()).Returns(graph);
        commandBuilder.Build(graph, job, "output").Returns(command);
        provider.ExportAsync(command, Arg.Any<Func<ExportProgressUpdate, CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ExportProviderResult.Success("output/file.mp4"));
        var pipeline = new ExportPipeline(timelineResolver, trackResolver, clipResolver, assetResolver,
            graphBuilder, commandBuilder, provider, Options.Create(new ExportOptions { OutputDirectory = "output" }));

        var result = await pipeline.ExecuteAsync(job, (_, _) => Task.CompletedTask);

        result.IsSuccess.Should().BeTrue();
        await provider.Received(1).ExportAsync(command, Arg.Any<Func<ExportProgressUpdate, CancellationToken, Task>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MockProvider_ShouldReportAllPhasesAndCreateManifest()
    {
        var output = Path.Combine(Path.GetTempPath(), $"aivideo-export-{Guid.NewGuid():N}");
        try
        {
            var provider = new MockExportProvider(Options.Create(new ExportOptions
            {
                OutputDirectory = output, TimeoutSeconds = 5, RetryCount = 0, MockStepDelayMilliseconds = 1
            }), NullLogger<MockExportProvider>.Instance);
            var updates = new List<ExportProgressUpdate>();
            var result = await provider.ExportAsync(EmptyCommand(output), (update, _) =>
            {
                updates.Add(update); return Task.CompletedTask;
            });

            result.IsSuccess.Should().BeTrue();
            File.Exists(result.OutputPath).Should().BeTrue();
            updates.Select(item => item.Status).Should().Contain(ExportStatus.Preparing)
                .And.Contain(ExportStatus.Rendering).And.Contain(ExportStatus.Muxing);
        }
        finally
        {
            if (Directory.Exists(output)) Directory.Delete(output, true);
        }
    }

    [Fact]
    public async Task MockProvider_ShouldHonorCancellation()
    {
        var provider = new MockExportProvider(Options.Create(new ExportOptions
        {
            OutputDirectory = Path.GetTempPath(), TimeoutSeconds = 5, RetryCount = 0, MockStepDelayMilliseconds = 50
        }), NullLogger<MockExportProvider>.Instance);
        using var source = new CancellationTokenSource();
        source.Cancel();
        var action = () => provider.ExportAsync(EmptyCommand(Path.GetTempPath()), (_, _) => Task.CompletedTask, source.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task MockProvider_ShouldReturnTimeoutAfterConfiguredRetries()
    {
        var provider = new MockExportProvider(Options.Create(new ExportOptions
        {
            OutputDirectory = Path.GetTempPath(), TimeoutSeconds = 1, RetryCount = 1, MockStepDelayMilliseconds = 200
        }), NullLogger<MockExportProvider>.Instance);

        var result = await provider.ExportAsync(
            EmptyCommand(Path.GetTempPath()), (_, _) => Task.CompletedTask);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("EXPORT_TIMEOUT");
    }

    [Fact]
    public async Task ExportQueue_ShouldEnqueueDequeueAndRemove()
    {
        var queue = new InMemoryExportQueue();
        await queue.EnqueueAsync(new ExportQueueItem("removed", DateTimeOffset.UtcNow));
        queue.Remove("removed").Should().BeTrue();
        await queue.EnqueueAsync(new ExportQueueItem("next", DateTimeOffset.UtcNow));
        var item = await queue.DequeueAsync();
        item.ExportJobId.Should().Be("next");
    }

    [Fact]
    public async Task Repository_ShouldInsertThroughMongoCollection()
    {
        var collection = Substitute.For<IMongoCollection<ExportJob>>();
        var repository = new ExportJobRepository(collection);
        var job = ExportJob.Create("r", "p", "t", "owner", TimeSpan.Zero, "1280x720", 24,
            VideoCodec.H264, AudioCodec.AAC, ContainerFormat.MP4);
        await repository.AddAsync(job);
        await collection.Received(1).InsertOneAsync(job, Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
    }

    private static Timeline BuildTimeline(out MediaAsset asset)
    {
        var timeline = Timeline.Create("p", "owner", "timeline");
        var video = timeline.AddTrack("video", TrackType.Video, "owner");
        var audio = timeline.AddTrack("audio", TrackType.Audio, "owner");
        asset = MediaAsset.Create("p", "owner", "source.mp4", "source.mp4", ".mp4", "video/mp4", 10,
            "storage/source.mp4", AssetType.Video);
        timeline.AddClip(video.Id, asset.Id, TimeSpan.Zero, TimeSpan.FromSeconds(1), "video", null, null, "owner");
        timeline.AddClip(audio.Id, asset.Id, TimeSpan.Zero, TimeSpan.FromSeconds(1), "audio", null, null, "owner");
        return timeline;
    }

    private static FFmpegCommandModel EmptyCommand(string output) => new(
        Array.Empty<FFmpegInputAsset>(), Array.Empty<VideoFilter>(), Array.Empty<AudioFilter>(),
        Array.Empty<SubtitleFilter>(), Array.Empty<TransitionFilter>(), Array.Empty<OverlayFilter>(),
        new FFmpegOutputOptions(output, "test.mp4", "1920x1080", 30, VideoCodec.H264, AudioCodec.AAC, ContainerFormat.MP4));
}
