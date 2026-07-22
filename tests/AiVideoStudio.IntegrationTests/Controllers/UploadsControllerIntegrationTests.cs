using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using AiVideoStudio.Api.Controllers.v1;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.Responses;
using FluentAssertions;
using NSubstitute;
using Xunit;
namespace AiVideoStudio.IntegrationTests.Controllers;

public class UploadsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _f; private readonly HttpClient _c;
    public UploadsControllerIntegrationTests(CustomWebApplicationFactory f) { _f = f; _c = f.CreateClient(); _f.CurrentUser.IsAuthenticated.Returns(true); _f.CurrentUser.UserId.Returns("owner"); _f.CurrentUser.Roles.Returns(Array.Empty<string>()); }
    [Fact] public async Task Start_ShouldCreateSession() { _f.ProjectRepository.GetByIdAsync("p", Arg.Any<CancellationToken>()).Returns(Project.Create("p", "owner")); var bytes = Encoding.UTF8.GetBytes("abcdef"); var response = await _c.PostAsJsonAsync("/api/v1/uploads/start", Request(bytes)); response.StatusCode.Should().Be(HttpStatusCode.Created); await _f.UploadSessionRepository.Received().AddAsync(Arg.Any<UploadSession>(), Arg.Any<CancellationToken>()); }
    [Fact]
    public async Task ChunkResumeMergeAndComplete_ShouldSucceed()
    {
        var bytes = Encoding.UTF8.GetBytes("abcdef"); var s = UploadSession.Create("p", "owner", "x.mp4", "video/mp4", 6, 2, Hash(bytes)); _f.UploadSessionRepository.GetByIdAsync(s.Id, Arg.Any<CancellationToken>()).Returns(s);
        (await Chunk(s.Id, 0, bytes[..3])).StatusCode.Should().Be(HttpStatusCode.OK); (await Chunk(s.Id, 0, bytes[..3])).StatusCode.Should().Be(HttpStatusCode.OK); (await Chunk(s.Id, 1, bytes[3..])).StatusCode.Should().Be(HttpStatusCode.OK);
        var done = await _c.PostAsync($"/api/v1/uploads/{s.Id}/complete", null); done.StatusCode.Should().Be(HttpStatusCode.OK); s.Status.Should().Be(UploadStatus.Completed); await _f.MediaAssetRepository.Received().AddAsync(Arg.Any<MediaAsset>(), Arg.Any<CancellationToken>());
    }
    [Fact] public async Task GetAndList_ShouldReturnData() { var s = Session(); _f.UploadSessionRepository.GetByIdAsync(s.Id, Arg.Any<CancellationToken>()).Returns(s); _f.ProjectRepository.GetByIdAsync("p", Arg.Any<CancellationToken>()).Returns(Project.Create("p", "owner")); _f.UploadSessionRepository.GetByProjectIdPagedAsync("p", 1, 20, Arg.Any<CancellationToken>()).Returns(new PagedResult<UploadSession>(new[] { s }, 1, 1, 20)); (await _c.GetAsync($"/api/v1/uploads/{s.Id}")).StatusCode.Should().Be(HttpStatusCode.OK); (await _c.GetAsync("/api/v1/projects/p/uploads")).StatusCode.Should().Be(HttpStatusCode.OK); }
    [Fact] public async Task Cancel_ShouldSucceed() { var s = Session(); _f.UploadSessionRepository.GetByIdAsync(s.Id, Arg.Any<CancellationToken>()).Returns(s); (await _c.PostAsync($"/api/v1/uploads/{s.Id}/cancel", null)).StatusCode.Should().Be(HttpStatusCode.OK); s.Status.Should().Be(UploadStatus.Cancelled); }
    [Fact] public async Task Retry_ShouldSucceed() { var s = Session(); s.Fail("x"); _f.UploadSessionRepository.GetByIdAsync(s.Id, Arg.Any<CancellationToken>()).Returns(s); (await _c.PostAsync($"/api/v1/uploads/{s.Id}/retry", null)).StatusCode.Should().Be(HttpStatusCode.OK); s.Status.Should().Be(UploadStatus.Pending); }
    [Fact] public async Task Get_ShouldReturnForbidden() { var s = UploadSession.Create("p", "other", "x.mp4", "video/mp4", 1, 1, new string('a', 64)); _f.UploadSessionRepository.GetByIdAsync(s.Id, Arg.Any<CancellationToken>()).Returns(s); (await _c.GetAsync($"/api/v1/uploads/{s.Id}")).StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task Get_ShouldReturnNotFound() { _f.UploadSessionRepository.GetByIdAsync("missing", Arg.Any<CancellationToken>()).Returns((UploadSession?)null); (await _c.GetAsync("/api/v1/uploads/missing")).StatusCode.Should().Be(HttpStatusCode.NotFound); }
    [Fact] public async Task Endpoint_ShouldReturnUnauthorized() { using var f = new UnauthenticatedExportFactory(); using var c = f.CreateClient(); (await c.GetAsync("/api/v1/uploads/anything")).StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    private async Task<HttpResponseMessage> Chunk(string id, int index, byte[] bytes) { using var form = new MultipartFormDataContent(); form.Add(new StringContent(index.ToString()), "ChunkIndex"); form.Add(new StringContent(Hash(bytes)), "Checksum"); form.Add(new ByteArrayContent(bytes), "Chunk", "chunk.part"); return await _c.PostAsync($"/api/v1/uploads/{id}/chunk", form); }
    private static StartUploadRequest Request(byte[] b) => new() { ProjectId = "p", FileName = "x.mp4", ContentType = "video/mp4", FileSize = b.Length, ChunkCount = 2, Checksum = Hash(b) };
    private static UploadSession Session() => UploadSession.Create("p", "owner", "x.mp4", "video/mp4", 1, 1, new string('a', 64)); private static string Hash(byte[] b) => Convert.ToHexString(SHA256.HashData(b)).ToLowerInvariant();
}
