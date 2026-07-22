using AiVideoStudio.Application.Features.Exports.Models;
using AiVideoStudio.Application.Interfaces.Export;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace AiVideoStudio.IntegrationTests;

public class ExportWorkerIntegrationTests
{
    [Fact]
    public async Task Worker_ShouldRunCompleteMockPipelineAndCreateOutputManifest()
    {
        using var factory = new CustomWebApplicationFactory();
        _ = factory.CreateClient();
        var timeline = Timeline.Create("project", "owner", "timeline");
        var track = timeline.AddTrack("video", TrackType.Video, "owner");
        var asset = MediaAsset.Create("project", "owner", "source.mp4", "source.mp4", ".mp4",
            "video/mp4", 10, "mock/source.mp4", AssetType.Video);
        timeline.AddClip(track.Id, asset.Id, TimeSpan.Zero, TimeSpan.FromSeconds(1), "clip", null, null, "owner");
        var job = ExportJob.Create("render", "project", timeline.Id, "owner", timeline.Duration,
            "1920x1080", 30, VideoCodec.H264, AudioCodec.AAC, ContainerFormat.MP4);
        factory.ExportJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        factory.TimelineRepository.GetByIdAsync(timeline.Id, Arg.Any<CancellationToken>()).Returns(timeline);
        factory.MediaAssetRepository.GetByIdAsync(asset.Id, Arg.Any<CancellationToken>()).Returns(asset);
        var queue = factory.Services.GetRequiredService<IExportQueue>();

        await queue.EnqueueAsync(new ExportQueueItem(job.Id, job.CreatedAt));

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (job.Status != ExportStatus.Completed)
            await Task.Delay(20, timeout.Token);

        job.OutputPath.Should().NotBeNull();
        File.Exists(job.OutputPath).Should().BeTrue();
        (await File.ReadAllTextAsync(job.OutputPath!, timeout.Token)).Should().Contain("InputAssets");
        File.Delete(job.OutputPath!);
    }
}
