using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.ApiContracts.V1.Projects.Requests;
using AiVideoStudio.Shared.Responses;
using AiVideoStudio.Application.Features.Projects.DTOs;
using FluentAssertions;
using NSubstitute;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AiVideoStudio.IntegrationTests.Controllers;

public class ProjectsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProjectsControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProject_WithValidPayload_ReturnsOk()
    {
        // Arrange
        var userId = "user_integration_123";
        _factory.CurrentUser.IsAuthenticated.Returns(true);
        _factory.CurrentUser.UserId.Returns(userId);

        var request = new CreateProjectRequest("Integration Test Project", "Desc", "http://thumb.jpg");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/projects", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProjectDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.Name.Should().Be("Integration Test Project");
        body.Data.OwnerId.Should().Be(userId);
    }

    [Fact]
    public async Task CreateProject_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        _factory.CurrentUser.IsAuthenticated.Returns(true);
        _factory.CurrentUser.UserId.Returns("user_1");

        var request = new CreateProjectRequest("");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/projects", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProjects_ReturnsPaginatedList()
    {
        // Arrange
        var userId = "user_integration_123";
        _factory.CurrentUser.IsAuthenticated.Returns(true);
        _factory.CurrentUser.UserId.Returns(userId);
        _factory.CurrentUser.Roles.Returns(new List<string> { "User" });

        var projects = new List<Project>
        {
            Project.Create("P1", userId),
            Project.Create("P2", userId)
        };
        var pagedResult = new PagedResult<Project>(projects, 2, 1, 10);

        _factory.ProjectRepository.GetPagedAsync(userId, false, 1, 10, null, null, false, null, Arg.Any<CancellationToken>())
            .Returns(pagedResult);

        // Act
        var response = await _client.GetAsync("/api/v1/projects?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProjectListResponse>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetProjectById_WhenExists_ReturnsOk()
    {
        // Arrange
        var userId = "user_integration_123";
        var project = Project.Create("Existing Project", userId);

        _factory.CurrentUser.IsAuthenticated.Returns(true);
        _factory.CurrentUser.UserId.Returns(userId);
        _factory.CurrentUser.Roles.Returns(new List<string> { "User" });

        _factory.ProjectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        // Act
        var response = await _client.GetAsync($"/api/v1/projects/{project.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProjectDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.Id.Should().Be(project.Id);
    }

    [Fact]
    public async Task GetProjectById_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _factory.CurrentUser.IsAuthenticated.Returns(true);
        _factory.CurrentUser.UserId.Returns("user_1");

        _factory.ProjectRepository.GetByIdAsync("missing_id", Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        // Act
        var response = await _client.GetAsync("/api/v1/projects/missing_id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProject_WhenOwner_ReturnsOk()
    {
        // Arrange
        var userId = "user_integration_123";
        var project = Project.Create("Original Name", userId);

        _factory.CurrentUser.IsAuthenticated.Returns(true);
        _factory.CurrentUser.UserId.Returns(userId);
        _factory.CurrentUser.Roles.Returns(new List<string> { "User" });

        _factory.ProjectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var updateRequest = new UpdateProjectRequest("New Updated Name", "New Desc", null, "Active");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/projects/{project.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProjectDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.Name.Should().Be("New Updated Name");
    }

    [Fact]
    public async Task DeleteProject_WhenOwner_ReturnsOk()
    {
        // Arrange
        var userId = "user_integration_123";
        var project = Project.Create("Project To Delete", userId);

        _factory.CurrentUser.IsAuthenticated.Returns(true);
        _factory.CurrentUser.UserId.Returns(userId);
        _factory.CurrentUser.Roles.Returns(new List<string> { "User" });

        _factory.ProjectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        // Act
        var response = await _client.DeleteAsync($"/api/v1/projects/{project.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().BeTrue();
    }
}
