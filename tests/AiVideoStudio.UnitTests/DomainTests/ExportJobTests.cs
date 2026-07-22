using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Events.Exports;
using FluentAssertions;
using Xunit;

namespace AiVideoStudio.UnitTests.DomainTests;

public class ExportJobTests
{
    [Fact]
    public void Create_ShouldInitializePendingAggregate()
    {
        var job = Create();
        job.Status.Should().Be(ExportStatus.Pending);
        job.Progress.Should().Be(0);
        job.Version.Should().Be(1);
        job.RetryCount.Should().Be(0);
    }

    [Fact]
    public void StateMachine_ShouldReachCompletedWithMonotonicVersion()
    {
        var job = Create();
        job.Start();
        job.UpdateProgress(10);
        job.MarkRendering();
        job.UpdateProgress(70);
        job.MarkMuxing();
        job.UpdateProgress(99);
        job.Complete("output.mp4");

        job.Status.Should().Be(ExportStatus.Completed);
        job.Progress.Should().Be(100);
        job.OutputPath.Should().Be("output.mp4");
        job.Version.Should().Be(8);
        job.DomainEvents.Should().ContainSingle(item => item is ExportStartedEvent);
        job.DomainEvents.Should().ContainSingle(item => item is ExportCompletedEvent);
    }

    [Fact]
    public void UpdateProgress_ShouldRejectDecreaseAndInvalidState()
    {
        var job = Create();
        var pending = () => job.UpdateProgress(1);
        pending.Should().Throw<InvalidOperationException>();
        job.Start();
        job.UpdateProgress(20);
        var decrease = () => job.UpdateProgress(10);
        decrease.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_ShouldStopPendingExportAndPublishEvent()
    {
        var job = Create();
        job.Cancel("owner");
        job.Status.Should().Be(ExportStatus.Cancelled);
        job.DomainEvents.Should().ContainSingle(item => item is ExportCancelledEvent);
        var again = () => job.Cancel("owner");
        again.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Retry_ShouldResetFailedExportAndIncrementCounters()
    {
        var job = Create();
        job.Start();
        job.Fail("failed", "TEST");
        job.Retry("owner");
        job.Status.Should().Be(ExportStatus.Pending);
        job.Progress.Should().Be(0);
        job.RetryCount.Should().Be(1);
        job.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Retry_ShouldEnforceMaximumRetryCount()
    {
        var job = Create(maxRetryCount: 0);
        job.Start();
        job.Fail("failed");
        var retry = () => job.Retry("owner");
        retry.Should().Throw<InvalidOperationException>().WithMessage("*Maximum retry*");
    }

    [Fact]
    public void Complete_ShouldRequireMuxingStateAndOutputPath()
    {
        var job = Create();
        var wrongState = () => job.Complete("output.mp4");
        wrongState.Should().Throw<InvalidOperationException>();
    }

    private static ExportJob Create(int maxRetryCount = 3) => ExportJob.Create(
        "render", "project", "timeline", "owner", TimeSpan.FromSeconds(10),
        "1920x1080", 30, VideoCodec.H264, AudioCodec.AAC, ContainerFormat.MP4, maxRetryCount);
}
