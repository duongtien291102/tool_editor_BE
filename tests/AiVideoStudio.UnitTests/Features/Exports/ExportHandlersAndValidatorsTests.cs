using AiVideoStudio.Application.Features.Exports;
using AiVideoStudio.Application.Features.Exports.DTOs;
using AiVideoStudio.Application.Features.Exports.Handlers;
using AiVideoStudio.Application.Features.Exports.Validators;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Application.Interfaces.Export;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AiVideoStudio.UnitTests.Features.Exports;

public class ExportHandlersAndValidatorsTests
{
    private readonly IExportJobRepository _exports = Substitute.For<IExportJobRepository>();
    private readonly IRenderJobRepository _renders = Substitute.For<IRenderJobRepository>();
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly ITimelineRepository _timelines = Substitute.For<ITimelineRepository>();
    private readonly IExportQueue _queue = Substitute.For<IExportQueue>();
    private readonly IExportJobCanceller _canceller = Substitute.For<IExportJobCanceller>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();

    [Fact]
    public async Task Create_ShouldPersistAndEnqueueValidExport()
    {
        var handler = CreateHandler();
        var project = Project.Create("project", "owner");
        var timeline = Timeline.Create("p1", "owner", "timeline");
        var render = RenderJob.Create("p1", "owner", RenderJobType.RenderTimeline, RenderProvider.Internal);
        _projects.GetByIdAsync("p1", Arg.Any<CancellationToken>()).Returns(project);
        _timelines.GetByIdAsync(timeline.Id, Arg.Any<CancellationToken>()).Returns(timeline);
        _renders.GetByIdAsync(render.Id, Arg.Any<CancellationToken>()).Returns(render);
        Authenticate("owner");
        _mapper.Map<ExportJobDto>(Arg.Any<ExportJob>()).Returns(call => ToDto(call.Arg<ExportJob>()));

        var result = await handler.Handle(new CreateExportJobCommand(
            render.Id, "p1", timeline.Id, VideoCodec.H264, AudioCodec.AAC, ContainerFormat.MP4), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _exports.Received(1).AddAsync(Arg.Any<ExportJob>(), Arg.Any<CancellationToken>());
        await _queue.Received(1).EnqueueAsync(Arg.Any<ExportQueueItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_ShouldReturnUnauthorizedForAnonymousUser()
    {
        var result = await CreateHandler().Handle(new CreateExportJobCommand(
            "r", "p", "t", VideoCodec.H264, AudioCodec.AAC, ContainerFormat.MP4), CancellationToken.None);
        result.Error.Should().Be(ExportErrors.Unauthorized);
    }

    [Fact]
    public async Task Create_ShouldReturnForbiddenForNonOwner()
    {
        _projects.GetByIdAsync("p1", Arg.Any<CancellationToken>()).Returns(Project.Create("project", "other"));
        Authenticate("owner");
        var result = await CreateHandler().Handle(new CreateExportJobCommand(
            "r", "p1", "t", VideoCodec.H264, AudioCodec.AAC, ContainerFormat.MP4), CancellationToken.None);
        result.Error.Should().Be(ExportErrors.Forbidden);
    }

    [Fact]
    public async Task Cancel_ShouldCancelAggregateQueueAndWorker()
    {
        var export = ExportJob.Create("r", "p", "t", "owner", TimeSpan.Zero, "1280x720", 24,
            VideoCodec.H264, AudioCodec.AAC, ContainerFormat.MP4);
        _exports.GetByIdAsync(export.Id, Arg.Any<CancellationToken>()).Returns(export);
        Authenticate("owner");

        var result = await CreateHandler().Handle(new CancelExportJobCommand(export.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        export.Status.Should().Be(ExportStatus.Cancelled);
        _queue.Received(1).Remove(export.Id);
        _canceller.Received(1).CancelActiveExport(export.Id);
    }

    [Fact]
    public async Task Retry_ShouldRequeueFailedExport()
    {
        var export = ExportJob.Create("r", "p", "t", "owner", TimeSpan.Zero, "1280x720", 24,
            VideoCodec.H264, AudioCodec.AAC, ContainerFormat.MP4);
        export.Start(); export.Fail("failure");
        _exports.GetByIdAsync(export.Id, Arg.Any<CancellationToken>()).Returns(export);
        Authenticate("owner");
        _mapper.Map<ExportJobDto>(export).Returns(ToDto(export));

        var result = await CreateHandler().Handle(new RetryExportJobCommand(export.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        export.Status.Should().Be(ExportStatus.Pending);
        await _queue.Received(1).EnqueueAsync(Arg.Any<ExportQueueItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validators_ShouldEnforceRequiredFieldsAndRanges()
    {
        new CreateExportJobValidator().Validate(new CreateExportJobCommand(
            "", "", "", (VideoCodec)999, (AudioCodec)999, (ContainerFormat)999, 11)).IsValid.Should().BeFalse();
        new RetryExportJobValidator().Validate(new RetryExportJobCommand("")).IsValid.Should().BeFalse();
        new CancelExportJobValidator().Validate(new CancelExportJobCommand("")).IsValid.Should().BeFalse();
        new UpdateExportProgressValidator().Validate(new UpdateExportProgressCommand("id", 100)).IsValid.Should().BeFalse();
    }

    private ExportCommandHandlers CreateHandler() => new(
        _exports, _renders, _projects, _timelines, _queue, _canceller, _user, _mapper);

    private void Authenticate(string id)
    {
        _user.IsAuthenticated.Returns(true);
        _user.UserId.Returns(id);
        _user.Roles.Returns(Array.Empty<string>());
    }

    private static ExportJobDto ToDto(ExportJob job) => new(
        job.Id, job.RenderJobId, job.ProjectId, job.TimelineId, job.OwnerId, job.Status,
        job.Progress, job.OutputPath, job.Duration, job.Resolution, job.FrameRate,
        job.VideoCodec, job.AudioCodec, job.Container, job.RetryCount, job.MaxRetryCount,
        job.ErrorCode, job.ErrorMessage, job.Version, job.StartedAt, job.CompletedAt,
        job.CreatedAt, job.UpdatedAt);
}
