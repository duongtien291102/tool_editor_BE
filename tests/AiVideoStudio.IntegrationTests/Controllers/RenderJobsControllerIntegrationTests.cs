using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AiVideoStudio.Api.Controllers.v1;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AiVideoStudio.IntegrationTests.Controllers;

public class RenderJobsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public RenderJobsControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateJob_Should_Return_Created_When_Valid()
    {
        // Arrange
        var request = new CreateRenderJobRequest
        {
            ProjectId = "p1",
            JobType = RenderJobType.RenderTimeline,
            Provider = RenderProvider.Internal,
            Priority = RenderPriority.High
        };

        var project = Project.Create("p1", "TestUser");
        _factory.ProjectRepository.GetByIdAsync("p1").Returns(project);
        _factory.CurrentUser.UserId.Returns("TestUser");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/render-jobs", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateJob_Should_Return_Forbid_When_Not_Owner()
    {
        // Arrange
        var request = new CreateRenderJobRequest { ProjectId = "p1", JobType = RenderJobType.RenderTimeline, Provider = RenderProvider.Internal };

        var project = Project.Create("p1", "OtherUser"); // Owned by someone else
        _factory.ProjectRepository.GetByIdAsync("p1").Returns(project);
        _factory.CurrentUser.UserId.Returns("TestUser");
        _factory.CurrentUser.Roles.Returns(System.Array.Empty<string>()); // Not admin

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/render-jobs", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetJobById_Should_Return_Ok_When_Exists()
    {
        // Arrange
        var job = RenderJob.Create("p1", "TestUser", RenderJobType.RenderTimeline, RenderProvider.Internal);
        _factory.RenderJobRepository.GetByIdAsync(job.Id).Returns(job);
        _factory.CurrentUser.UserId.Returns("TestUser");

        // Act
        var response = await _client.GetAsync($"/api/v1/render-jobs/{job.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetJobById_Should_Return_NotFound_When_Missing()
    {
        // Arrange
        _factory.RenderJobRepository.GetByIdAsync("invalid").Returns((RenderJob?)null);
        _factory.CurrentUser.UserId.Returns("TestUser");

        // Act
        var response = await _client.GetAsync("/api/v1/render-jobs/invalid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelJob_Should_Return_Ok_When_Valid()
    {
        // Arrange
        var job = RenderJob.Create("p1", "TestUser", RenderJobType.RenderTimeline, RenderProvider.Internal);
        _factory.RenderJobRepository.GetByIdAsync(job.Id).Returns(job);
        _factory.CurrentUser.UserId.Returns("TestUser");

        // Act
        var response = await _client.PostAsync($"/api/v1/render-jobs/{job.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CancelJob_Should_Return_BadRequest_When_Already_Completed()
    {
        // Arrange
        var job = RenderJob.Create("p1", "TestUser", RenderJobType.RenderTimeline, RenderProvider.Internal);
        job.Queue();
        job.Start();
        job.Complete(); // Now completed

        _factory.RenderJobRepository.GetByIdAsync(job.Id).Returns(job);
        _factory.CurrentUser.UserId.Returns("TestUser");

        // Act
        var response = await _client.PostAsync($"/api/v1/render-jobs/{job.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("AlreadyCompleted");
    }
}
