using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Application.Features.Timelines;
using AiVideoStudio.Application.Features.Timelines.DTOs;
using AiVideoStudio.Application.Features.Timelines.Handlers;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Application.Interfaces;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AiVideoStudio.UnitTests.Features.Timelines;

public class TimelineCommandsHandlerTests
{
    private readonly ITimelineRepository _timelineRepo = Substitute.For<ITimelineRepository>();
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private readonly TimelineCommandsHandler _timelineHandler;
    private readonly TrackCommandsHandler _trackHandler;
    private readonly ClipCommandsHandler _clipHandler;
    private readonly TimelineQueriesHandler _queryHandler;

    public TimelineCommandsHandlerTests()
    {
        _timelineHandler = new TimelineCommandsHandler(_timelineRepo, _projectRepo, _mapper, _currentUser);
        _trackHandler = new TrackCommandsHandler(_timelineRepo, _mapper, _currentUser);
        _clipHandler = new ClipCommandsHandler(_timelineRepo, _mapper, _currentUser);
        _queryHandler = new TimelineQueriesHandler(_timelineRepo, _mapper, _currentUser, _projectRepo);

        _currentUser.UserId.Returns("u1");
        _currentUser.Roles.Returns(new List<string> { "User" });
    }

    [Fact]
    public async Task CreateTimeline_WhenAlreadyExists_ReturnsAlreadyExists()
    {
        var cmd = new CreateTimelineCommand("p1", "Timeline 1");
        var project = Project.Create("P1", "u1");
        _projectRepo.GetByIdAsync("p1", Arg.Any<CancellationToken>()).Returns(project);
        
        var existingTimeline = Timeline.Create("p1", "u1", "Existing");
        _timelineRepo.GetByProjectIdAsync("p1", Arg.Any<CancellationToken>()).Returns(existingTimeline);

        var result = await _timelineHandler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(TimelineErrors.AlreadyExists);
    }

    [Fact]
    public async Task CreateTimeline_Should_Succeed_When_Timeline_Does_Not_Exist()
    {
        var cmd = new CreateTimelineCommand("p1", "Timeline 1");
        var project = Project.Create("P1", "u1");
        _projectRepo.GetByIdAsync("p1", Arg.Any<CancellationToken>()).Returns(project);
        
        _timelineRepo.GetByProjectIdAsync("p1", Arg.Any<CancellationToken>()).Returns((Timeline?)null);
        _mapper.Map<TimelineDto>(Arg.Any<Timeline>()).Returns(new TimelineDto("t1", "p1", "u1", "Timeline 1", 1, 30, 1920, 1080, TimeSpan.Zero, DateTimeOffset.UtcNow, null, new List<TrackDto>()));

        var result = await _timelineHandler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _timelineRepo.Received(1).AddAsync(Arg.Any<Timeline>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTimeline_WhenVersionConflict_ReturnsVersionConflict()
    {
        var timeline = Timeline.Create("p1", "u1", "Original");
        _timelineRepo.GetByIdAsync("t1", Arg.Any<CancellationToken>()).Returns(timeline);
        _timelineRepo.UpdateAsync(timeline, timeline.Version, Arg.Any<CancellationToken>()).Returns(false);

        var cmd = new UpdateTimelineCommand("t1", "Updated", 30, 1920, 1080);
        var result = await _timelineHandler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(TimelineErrors.VersionConflict);
    }

    [Fact]
    public async Task UpdateTimeline_WhenUnauthorized_ReturnsUnauthorized()
    {
        var timeline = Timeline.Create("p1", "owner_user", "Original");
        _currentUser.UserId.Returns("other_user");
        _currentUser.Roles.Returns(new List<string> { "User" });
        _timelineRepo.GetByIdAsync("t1", Arg.Any<CancellationToken>()).Returns(timeline);

        var cmd = new UpdateTimelineCommand("t1", "Updated", 30, 1920, 1080);
        var result = await _timelineHandler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(AuthErrors.Unauthorized);
    }

    [Fact]
    public async Task DeleteTimeline_WhenVersionConflict_ReturnsVersionConflict()
    {
        var timeline = Timeline.Create("p1", "u1", "Original");
        _timelineRepo.GetByIdAsync("t1", Arg.Any<CancellationToken>()).Returns(timeline);
        _timelineRepo.UpdateAsync(timeline, timeline.Version, Arg.Any<CancellationToken>()).Returns(false);

        var cmd = new DeleteTimelineCommand("t1");
        var result = await _timelineHandler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(TimelineErrors.VersionConflict);
    }

    [Fact]
    public async Task AutoSave_WithNoChanges_ShouldNotIncrementVersion()
    {
        var timeline = Timeline.Create("p1", "u1", "Timeline 1");
        var dto = new TimelineDto("t1", "p1", "u1", "Timeline 1", 1, 30, 1920, 1080, TimeSpan.Zero, DateTimeOffset.UtcNow, null, new List<TrackDto>());
        
        var cmd = new AutoSaveTimelineCommand("t1", dto);
        _timelineRepo.GetByIdAsync("t1", Arg.Any<CancellationToken>()).Returns(timeline);
        
        _mapper.Map<TimelineDto>(timeline).Returns(dto);

        var result = await _timelineHandler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _timelineRepo.DidNotReceive().UpdateAsync(Arg.Any<Timeline>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AutoSave_WithChanges_ShouldIncrementVersion()
    {
        var timeline = Timeline.Create("p1", "u1", "Timeline 1");
        var currentDto = new TimelineDto("t1", "p1", "u1", "Timeline 1", 1, 30, 1920, 1080, TimeSpan.Zero, DateTimeOffset.UtcNow, null, new List<TrackDto>());
        
        var incomingDto = new TimelineDto("t1", "p1", "u1", "Timeline Changed", 1, 30, 1920, 1080, TimeSpan.Zero, DateTimeOffset.UtcNow, null, new List<TrackDto>());
        
        var cmd = new AutoSaveTimelineCommand("t1", incomingDto);
        _timelineRepo.GetByIdAsync("t1", Arg.Any<CancellationToken>()).Returns(timeline);
        
        _mapper.Map<TimelineDto>(timeline).Returns(currentDto);

        _timelineRepo.UpdateAsync(timeline, timeline.Version, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _timelineHandler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _timelineRepo.Received(1).UpdateAsync(timeline, 1, Arg.Any<CancellationToken>());
    }

    // Query Handler Tests
    [Fact]
    public async Task GetTimeline_NotFound()
    {
        _timelineRepo.GetByIdAsync("missing_t", Arg.Any<CancellationToken>()).Returns((Timeline?)null);

        var query = new GetTimelineQuery("missing_t");
        var result = await _queryHandler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(TimelineErrors.NotFound);
    }

    [Fact]
    public async Task GetTimeline_Unauthorized()
    {
        var timeline = Timeline.Create("p1", "owner_user", "Timeline 1");
        var project = Project.Create("P1", "owner_user");

        _currentUser.UserId.Returns("other_user");
        _currentUser.Roles.Returns(new List<string> { "User" });

        _timelineRepo.GetByIdAsync("t1", Arg.Any<CancellationToken>()).Returns(timeline);
        _projectRepo.GetByIdAsync("p1", Arg.Any<CancellationToken>()).Returns(project);

        var query = new GetTimelineQuery("t1");
        var result = await _queryHandler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(AuthErrors.Unauthorized);
    }

    // Track Command Handler Tests
    [Fact]
    public async Task AddTrack_NotFound()
    {
        _timelineRepo.GetByIdAsync("missing_t", Arg.Any<CancellationToken>()).Returns((Timeline?)null);

        var cmd = new AddTrackCommand("missing_t", "V1", TrackType.Video);
        var result = await _trackHandler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(TimelineErrors.NotFound);
    }

    // Clip Command Handler Tests
    [Fact]
    public async Task AddClip_NotFound()
    {
        _timelineRepo.GetByIdAsync("missing_t", Arg.Any<CancellationToken>()).Returns((Timeline?)null);

        var cmd = new AddClipCommand("missing_t", "track1", "asset1", TimeSpan.Zero, TimeSpan.FromSeconds(5), "Clip 1", null, null);
        var result = await _clipHandler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(TimelineErrors.NotFound);
    }

    [Fact]
    public async Task MoveClip_NotFound()
    {
        _timelineRepo.GetByIdAsync("missing_t", Arg.Any<CancellationToken>()).Returns((Timeline?)null);

        var cmd = new MoveClipCommand("missing_t", "clip1", "track2", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
        var result = await _clipHandler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(TimelineErrors.NotFound);
    }

    [Fact]
    public async Task ResizeClip_NotFound()
    {
        _timelineRepo.GetByIdAsync("missing_t", Arg.Any<CancellationToken>()).Returns((Timeline?)null);

        var cmd = new ResizeClipCommand("missing_t", "clip1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10));
        var result = await _clipHandler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(TimelineErrors.NotFound);
    }

    [Fact]
    public async Task DeleteClip_NotFound()
    {
        _timelineRepo.GetByIdAsync("missing_t", Arg.Any<CancellationToken>()).Returns((Timeline?)null);

        var cmd = new DeleteClipCommand("missing_t", "clip1");
        var result = await _clipHandler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(TimelineErrors.NotFound);
    }
}
