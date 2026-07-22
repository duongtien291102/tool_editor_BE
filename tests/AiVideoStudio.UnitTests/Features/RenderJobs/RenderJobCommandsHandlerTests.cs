using System;
using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Application.Features.RenderJobs;
using AiVideoStudio.Application.Features.RenderJobs.DTOs;
using AiVideoStudio.Application.Features.RenderJobs.Handlers;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Application.Interfaces.Render;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AiVideoStudio.UnitTests.Features.RenderJobs;

public class RenderJobCommandsHandlerTests
{
    private readonly IRenderJobRepository _repo = Substitute.For<IRenderJobRepository>();
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly IRenderQueue _queue = Substitute.For<IRenderQueue>();
    private readonly IRenderJobCanceller _canceller = Substitute.For<IRenderJobCanceller>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();

    private readonly RenderJobCommandsHandler _handler;

    public RenderJobCommandsHandlerTests()
    {
        _handler = new RenderJobCommandsHandler(_repo, _projectRepo, _queue, _canceller, _currentUser, _mapper);
    }

    [Fact]
    public async Task Create_Should_Succeed_And_Enqueue()
    {
        // Arrange
        var cmd = new CreateRenderJobCommand("p1", RenderJobType.RenderTimeline, RenderProvider.Internal, RenderPriority.High, 3, null, null, null);
        var project = Project.Create("p1", "u1", "desc", null);
        _projectRepo.GetByIdAsync("p1").Returns(project);
        _currentUser.UserId.Returns("u1");

        _mapper.Map<RenderJobDto>(Arg.Any<RenderJob>()).Returns(new RenderJobDto("j1", "p1", null, null, "u1", RenderJobType.RenderTimeline, RenderProvider.Internal, RenderJobStatus.Queued, RenderPriority.High, 0, 0, 3, null, null, null, null, null, null, DateTimeOffset.UtcNow, null));

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).AddAsync(Arg.Any<RenderJob>(), Arg.Any<CancellationToken>());
        await _queue.Received(1).EnqueueAsync(Arg.Is<QueueItem>(q => q.Priority == RenderPriority.High), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_Should_Succeed_Remove_From_Queue_And_Cancel_Worker()
    {
        // Arrange
        var cmd = new CancelRenderJobCommand("j1");
        var job = RenderJob.Create("p1", "u1", RenderJobType.RenderTimeline, RenderProvider.Internal);
        _repo.GetByIdAsync("j1").Returns(job);
        _currentUser.UserId.Returns("u1");

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(RenderJobStatus.Cancelled);
        await _queue.Received(1).RemoveAsync(job.Id, Arg.Any<CancellationToken>());
        _canceller.Received(1).CancelActiveJob(job.Id);
        await _repo.Received(1).UpdateAsync(job, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Retry_Should_Succeed_And_Reenqueue()
    {
        // Arrange
        var cmd = new RetryRenderJobCommand("j1");
        var job = RenderJob.Create("p1", "u1", RenderJobType.RenderTimeline, RenderProvider.Internal);
        job.Queue();
        job.Start();
        job.Fail("Test");
        
        _repo.GetByIdAsync("j1").Returns(job);
        _currentUser.UserId.Returns("u1");

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(RenderJobStatus.Queued);
        job.RetryCount.Should().Be(1);
        await _queue.Received(1).EnqueueAsync(Arg.Any<QueueItem>(), Arg.Any<CancellationToken>());
    }
}
