using AiVideoStudio.Application.Features.Media.DTOs;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.ApiContracts.V1.Media.Requests;
using AiVideoStudio.Shared.Responses;
using AiVideoStudio.Application.Storage;
using FluentAssertions;
using NSubstitute;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace AiVideoStudio.IntegrationTests.Controllers;

public class MediaControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MediaControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UploadMedia_WithMultipartFormData_ReturnsOk()
    {
        // Arrange
        var userId = "user_integration_123";
        var project = Project.Create("Integration Project", userId);

        _factory.CurrentUser.IsAuthenticated.Returns(true);
        _factory.CurrentUser.UserId.Returns(userId);
        _factory.CurrentUser.Roles.Returns(new List<string> { "User" });

        _factory.ProjectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("test file content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "file", "test_upload.jpg");
        content.Add(new StringContent(project.Id), "projectId");

        // Act
        var response = await _client.PostAsync("/api/v1/media/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<MediaDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.OriginalFileName.Should().Be("test_upload.jpg");
        body.Data.ProjectId.Should().Be(project.Id);
    }

    [Fact]
    public async Task GetMediaById_WhenExists_ReturnsOk()
    {
        // Arrange
        var userId = "user_integration_123";
        var project = Project.Create("Integration Project", userId);
        var mediaAsset = MediaAsset.Create(project.Id, userId, "media_file.jpg", "media_file.jpg", ".jpg", "image/jpeg", 1024, "path", AssetType.Image);

        _factory.CurrentUser.IsAuthenticated.Returns(true);
        _factory.CurrentUser.UserId.Returns(userId);
        _factory.CurrentUser.Roles.Returns(new List<string> { "User" });

        _factory.MediaAssetRepository.GetByIdAsync(mediaAsset.Id, Arg.Any<CancellationToken>())
            .Returns(mediaAsset);
        _factory.ProjectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        // Act
        var response = await _client.GetAsync($"/api/v1/media/{mediaAsset.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<MediaDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.Id.Should().Be(mediaAsset.Id);
    }

    [Fact]
    public async Task GetMediaContent_WhenOwner_StreamsFromConfiguredStorage()
    {
        var userId = "media_content_owner";
        var project = Project.Create("Delivery Project", userId);
        var storage = _factory.Services.GetRequiredService<IStorageProvider>();
        var bytes = Encoding.UTF8.GetBytes("browser-delivery");
        var path = $"delivery/{System.Guid.NewGuid():N}.txt";
        await using (var source = new MemoryStream(bytes))
        {
            await storage.UploadAsync(string.Empty, path, source, "text/plain");
        }

        var media = MediaAsset.Create(project.Id, userId, "asset.txt", "asset.txt", ".txt", "text/plain", bytes.Length, path, AssetType.Other);
        _factory.CurrentUser.IsAuthenticated.Returns(true);
        _factory.CurrentUser.UserId.Returns(userId);
        _factory.CurrentUser.Roles.Returns(new List<string> { "User" });
        _factory.MediaAssetRepository.GetByIdAsync(media.Id, Arg.Any<CancellationToken>()).Returns(media);
        _factory.ProjectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var response = await _client.GetAsync($"/api/v1/media/{media.Id}/content?variant=original");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        (await response.Content.ReadAsStringAsync()).Should().Be("browser-delivery");
        response.Headers.CacheControl!.Private.Should().BeTrue();
        await storage.DeleteAsync(string.Empty, path);
    }

    [Fact]
    public async Task GetMediaThumbnail_WhenMetadataIsMissing_ReturnsNotFound()
    {
        var userId = "media_thumbnail_owner";
        var project = Project.Create("Delivery Project", userId);
        var media = MediaAsset.Create(project.Id, userId, "asset.jpg", "asset.jpg", ".jpg", "image/jpeg", 10, "asset.jpg", AssetType.Image);
        _factory.CurrentUser.IsAuthenticated.Returns(true);
        _factory.CurrentUser.UserId.Returns(userId);
        _factory.CurrentUser.Roles.Returns(new List<string> { "User" });
        _factory.MediaAssetRepository.GetByIdAsync(media.Id, Arg.Any<CancellationToken>()).Returns(media);
        _factory.ProjectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var response = await _client.GetAsync($"/api/v1/media/{media.Id}/content?variant=thumbnail");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProjectMedia_ReturnsPaginatedList()
    {
        // Arrange
        var userId = "user_integration_123";
        var project = Project.Create("Integration Project", userId);

        _factory.CurrentUser.IsAuthenticated.Returns(true);
        _factory.CurrentUser.UserId.Returns(userId);
        _factory.CurrentUser.Roles.Returns(new List<string> { "User" });

        _factory.ProjectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var mediaList = new List<MediaAsset>
        {
            MediaAsset.Create(project.Id, userId, "m1.jpg", "m1.jpg", ".jpg", "image/jpeg", 1024, "p1", AssetType.Image),
            MediaAsset.Create(project.Id, userId, "m2.jpg", "m2.jpg", ".jpg", "image/jpeg", 2048, "p2", AssetType.Image)
        };
        var pagedResult = new PagedResult<MediaAsset>(mediaList, 2, 1, 10);

        _factory.MediaAssetRepository.GetPagedByProjectIdAsync(project.Id, 1, 10, null, null, false, null, null, Arg.Any<CancellationToken>())
            .Returns(pagedResult);

        // Act
        var response = await _client.GetAsync($"/api/v1/projects/{project.Id}/media?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<MediaListResponse>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateMedia_WhenOwner_ReturnsOk()
    {
        // Arrange
        var userId = "user_integration_123";
        var mediaAsset = MediaAsset.Create("p1", userId, "old.jpg", "old.jpg", ".jpg", "image/jpeg", 1024, "path", AssetType.Image);

        _factory.CurrentUser.IsAuthenticated.Returns(true);
        _factory.CurrentUser.UserId.Returns(userId);
        _factory.CurrentUser.Roles.Returns(new List<string> { "User" });

        _factory.MediaAssetRepository.GetByIdAsync(mediaAsset.Id, Arg.Any<CancellationToken>())
            .Returns(mediaAsset);

        var updateRequest = new UpdateMediaRequest
        {
            FileName = "renamed.jpg",
            Width = 1280,
            Height = 720
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/media/{mediaAsset.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<MediaDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.FileName.Should().Be("renamed.jpg");
    }

    [Fact]
    public async Task DeleteMedia_WhenOwner_ReturnsOk()
    {
        // Arrange
        var userId = "user_integration_123";
        var mediaAsset = MediaAsset.Create("p1", userId, "to_delete.jpg", "to_delete.jpg", ".jpg", "image/jpeg", 1024, "path", AssetType.Image);

        _factory.CurrentUser.IsAuthenticated.Returns(true);
        _factory.CurrentUser.UserId.Returns(userId);
        _factory.CurrentUser.Roles.Returns(new List<string> { "User" });

        _factory.MediaAssetRepository.GetByIdAsync(mediaAsset.Id, Arg.Any<CancellationToken>())
            .Returns(mediaAsset);

        // Act
        var response = await _client.DeleteAsync($"/api/v1/media/{mediaAsset.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().BeTrue();
    }
}
