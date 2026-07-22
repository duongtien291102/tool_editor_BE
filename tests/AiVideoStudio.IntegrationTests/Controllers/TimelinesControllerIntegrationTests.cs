using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Application.Features.Timelines.DTOs;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.ApiContracts.V1.Timelines.Requests;
using AiVideoStudio.Shared.Responses;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AiVideoStudio.IntegrationTests.Controllers;

public class TimelinesControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TimelinesControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void SetupAuthenticatedUser(string userId = "user_integration_123", bool isAdmin = false)
    {
        _factory.CurrentUser.IsAuthenticated.Returns(true);
        _factory.CurrentUser.UserId.Returns(userId);
        _factory.CurrentUser.Roles.Returns(isAdmin ? new List<string> { "Admin" } : new List<string> { "User" });
    }

    [Fact]
    public async Task CreateTimeline_WithValidPayload_ReturnsCreated()
    {
        var userId = "user_1";
        SetupAuthenticatedUser(userId);

        var project = Project.Create("P1", userId);
        _factory.ProjectRepository.GetByIdAsync("p1", Arg.Any<CancellationToken>()).Returns(project);
        _factory.TimelineRepository.GetByProjectIdAsync("p1", Arg.Any<CancellationToken>()).Returns((Timeline?)null);

        var request = new CreateTimelineRequest { ProjectId = "p1", Name = "Timeline 1", FrameRate = 30.0, ResolutionWidth = 1920, ResolutionHeight = 1080 };
        var response = await _client.PostAsJsonAsync("/api/v1/timelines", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<TimelineDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.Name.Should().Be("Timeline 1");
    }

    [Fact]
    public async Task CreateTimeline_OneProjectOnlyOneTimeline_ReturnsConflict()
    {
        var userId = "user_1";
        SetupAuthenticatedUser(userId);

        var project = Project.Create("P1", userId);
        var existingTimeline = Timeline.Create("p1", userId, "Existing Timeline");

        _factory.ProjectRepository.GetByIdAsync("p1", Arg.Any<CancellationToken>()).Returns(project);
        _factory.TimelineRepository.GetByProjectIdAsync("p1", Arg.Any<CancellationToken>()).Returns(existingTimeline);

        var request = new CreateTimelineRequest { ProjectId = "p1", Name = "Timeline Duplicate", FrameRate = 30.0, ResolutionWidth = 1920, ResolutionHeight = 1080 };
        var response = await _client.PostAsJsonAsync("/api/v1/timelines", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.Message.Should().Be("Conflict");
    }

    [Fact]
    public async Task GetTimelineByProject_WhenAuthorized_ReturnsOk()
    {
        var userId = "user_1";
        SetupAuthenticatedUser(userId);

        var project = Project.Create("P1", userId);
        var timeline = Timeline.Create(project.Id, userId, "Timeline 1");

        _factory.ProjectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _factory.TimelineRepository.GetByProjectIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(timeline);

        var response = await _client.GetAsync($"/api/v1/projects/{project.Id}/timeline");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<TimelineDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.Name.Should().Be("Timeline 1");
    }

    [Fact]
    public async Task GetTimelineById_WhenNotFound_ReturnsNotFound()
    {
        SetupAuthenticatedUser("user_1");
        _factory.TimelineRepository.GetByIdAsync("missing_id", Arg.Any<CancellationToken>()).Returns((Timeline?)null);

        var response = await _client.GetAsync("/api/v1/timelines/missing_id");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.Message.Should().Be("Not found");
    }

    [Fact]
    public async Task UpdateTimeline_WhenVersionConflict_ReturnsConflict()
    {
        var userId = "user_1";
        SetupAuthenticatedUser(userId);

        var timeline = Timeline.Create("p1", userId, "Original Timeline");
        _factory.TimelineRepository.GetByIdAsync(timeline.Id, Arg.Any<CancellationToken>()).Returns(timeline);
        _factory.TimelineRepository.UpdateAsync(Arg.Any<Timeline>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(false);

        var request = new UpdateTimelineRequest { Name = "Updated Timeline", FrameRate = 60.0, ResolutionWidth = 1920, ResolutionHeight = 1080 };
        var response = await _client.PutAsJsonAsync($"/api/v1/timelines/{timeline.Id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Message.Should().Be("Conflict");
    }

    [Fact]
    public async Task DeleteTimeline_SoftDelete_ReturnsOk()
    {
        var userId = "user_1";
        SetupAuthenticatedUser(userId);

        var timeline = Timeline.Create("p1", userId, "Timeline To Delete");
        _factory.TimelineRepository.GetByIdAsync(timeline.Id, Arg.Any<CancellationToken>()).Returns(timeline);
        _factory.TimelineRepository.UpdateAsync(Arg.Any<Timeline>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);

        var response = await _client.DeleteAsync($"/api/v1/timelines/{timeline.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AddTrack_ReturnsCreated()
    {
        var userId = "user_1";
        SetupAuthenticatedUser(userId);

        var timeline = Timeline.Create("p1", userId, "Timeline");
        _factory.TimelineRepository.GetByIdAsync(timeline.Id, Arg.Any<CancellationToken>()).Returns(timeline);
        _factory.TimelineRepository.UpdateAsync(Arg.Any<Timeline>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);

        var request = new AddTrackRequest { Name = "Video Track 1", TrackType = (int)TrackType.Video };
        var response = await _client.PostAsJsonAsync($"/api/v1/timelines/{timeline.Id}/tracks", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<TrackDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.Name.Should().Be("Video Track 1");
    }

    [Fact]
    public async Task RemoveTrack_WhenTrackContainsClips_ReturnsConflict()
    {
        var userId = "user_1";
        SetupAuthenticatedUser(userId);

        var timeline = Timeline.Create("p1", userId, "Timeline");
        var track = timeline.AddTrack("V1", TrackType.Video, userId);
        timeline.AddClip(track.Id, "asset1", TimeSpan.Zero, TimeSpan.FromSeconds(5), "Clip 1", null, null, userId);

        _factory.TimelineRepository.GetByIdAsync(timeline.Id, Arg.Any<CancellationToken>()).Returns(timeline);

        var response = await _client.DeleteAsync($"/api/v1/timelines/{timeline.Id}/tracks/{track.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Message.Should().Be("Conflict");
    }

    [Fact]
    public async Task AddClip_VideoOverlap_ReturnsConflict()
    {
        var userId = "user_1";
        SetupAuthenticatedUser(userId);

        var timeline = Timeline.Create("p1", userId, "Timeline");
        var track = timeline.AddTrack("V1", TrackType.Video, userId);
        timeline.AddClip(track.Id, "asset1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), "Clip 1", null, null, userId);

        _factory.TimelineRepository.GetByIdAsync(timeline.Id, Arg.Any<CancellationToken>()).Returns(timeline);

        var request = new AddClipRequest { TrackId = track.Id, AssetId = "asset2", StartFrame = TimeSpan.FromSeconds(5), EndFrame = TimeSpan.FromSeconds(15), Name = "Clip 2" };
        var response = await _client.PostAsJsonAsync($"/api/v1/timelines/{timeline.Id}/clips", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Message.Should().Be("Conflict");
    }

    [Fact]
    public async Task AddClip_AudioOverlap_ReturnsConflict()
    {
        var userId = "user_1";
        SetupAuthenticatedUser(userId);

        var timeline = Timeline.Create("p1", userId, "Timeline");
        var track = timeline.AddTrack("A1", TrackType.Audio, userId);
        timeline.AddClip(track.Id, "asset1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), "Clip 1", null, null, userId);

        _factory.TimelineRepository.GetByIdAsync(timeline.Id, Arg.Any<CancellationToken>()).Returns(timeline);

        var request = new AddClipRequest { TrackId = track.Id, AssetId = "asset2", StartFrame = TimeSpan.FromSeconds(5), EndFrame = TimeSpan.FromSeconds(15), Name = "Clip 2" };
        var response = await _client.PostAsJsonAsync($"/api/v1/timelines/{timeline.Id}/clips", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Message.Should().Be("Conflict");
    }

    [Fact]
    public async Task AddClip_OverlayOverlap_ReturnsCreated()
    {
        var userId = "user_1";
        SetupAuthenticatedUser(userId);

        var timeline = Timeline.Create("p1", userId, "Timeline");
        var track = timeline.AddTrack("O1", TrackType.Overlay, userId);
        timeline.AddClip(track.Id, "asset1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), "Clip 1", null, null, userId);

        _factory.TimelineRepository.GetByIdAsync(timeline.Id, Arg.Any<CancellationToken>()).Returns(timeline);
        _factory.TimelineRepository.UpdateAsync(Arg.Any<Timeline>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);

        var request = new AddClipRequest { TrackId = track.Id, AssetId = "asset2", StartFrame = TimeSpan.FromSeconds(5), EndFrame = TimeSpan.FromSeconds(15), Name = "Clip 2" };
        var response = await _client.PostAsJsonAsync($"/api/v1/timelines/{timeline.Id}/clips", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ClipDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AddClip_SubtitleOverlap_ReturnsCreated()
    {
        var userId = "user_1";
        SetupAuthenticatedUser(userId);

        var timeline = Timeline.Create("p1", userId, "Timeline");
        var track = timeline.AddTrack("S1", TrackType.Subtitle, userId);
        timeline.AddClip(track.Id, "asset1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), "Clip 1", null, null, userId);

        _factory.TimelineRepository.GetByIdAsync(timeline.Id, Arg.Any<CancellationToken>()).Returns(timeline);
        _factory.TimelineRepository.UpdateAsync(Arg.Any<Timeline>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);

        var request = new AddClipRequest { TrackId = track.Id, AssetId = "asset2", StartFrame = TimeSpan.FromSeconds(5), EndFrame = TimeSpan.FromSeconds(15), Name = "Clip 2" };
        var response = await _client.PostAsJsonAsync($"/api/v1/timelines/{timeline.Id}/clips", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ClipDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AddClip_EffectOverlap_ReturnsCreated()
    {
        var userId = "user_1";
        SetupAuthenticatedUser(userId);

        var timeline = Timeline.Create("p1", userId, "Timeline");
        var track = timeline.AddTrack("E1", TrackType.Effect, userId);
        timeline.AddClip(track.Id, "asset1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), "Clip 1", null, null, userId);

        _factory.TimelineRepository.GetByIdAsync(timeline.Id, Arg.Any<CancellationToken>()).Returns(timeline);
        _factory.TimelineRepository.UpdateAsync(Arg.Any<Timeline>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);

        var request = new AddClipRequest { TrackId = track.Id, AssetId = "asset2", StartFrame = TimeSpan.FromSeconds(5), EndFrame = TimeSpan.FromSeconds(15), Name = "Clip 2" };
        var response = await _client.PostAsJsonAsync($"/api/v1/timelines/{timeline.Id}/clips", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ClipDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AutoSaveTimeline_WithNoChanges_ReturnsOk()
    {
        var userId = "user_1";
        SetupAuthenticatedUser(userId);

        var timeline = Timeline.Create("p1", userId, "Timeline 1");
        var dto = new TimelineDto(timeline.Id, "p1", userId, "Timeline 1", 1, 30, 1920, 1080, TimeSpan.Zero, DateTimeOffset.UtcNow, null, new List<TrackDto>());

        _factory.TimelineRepository.GetByIdAsync(timeline.Id, Arg.Any<CancellationToken>()).Returns(timeline);

        var request = new AutoSaveTimelineRequest { Data = dto };
        var response = await _client.PostAsJsonAsync($"/api/v1/timelines/{timeline.Id}/autosave", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<TimelineDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
    }
}
