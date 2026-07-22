using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AiVideoStudio.Api.Controllers.v1;
using AiVideoStudio.Application.Features.Exports.DTOs;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.Responses;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace AiVideoStudio.IntegrationTests.Controllers;

public class ExportControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ExportControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.CurrentUser.IsAuthenticated.Returns(true);
        _factory.CurrentUser.UserId.Returns("owner");
        _factory.CurrentUser.Roles.Returns(Array.Empty<string>());
    }

    [Fact]
    public async Task Create_ShouldReturnCreated()
    {
        var timeline = Timeline.Create("p1", "owner", "timeline");
        var render = RenderJob.Create("p1", "owner", RenderJobType.RenderTimeline, RenderProvider.Internal);
        _factory.ProjectRepository.GetByIdAsync("p1", Arg.Any<CancellationToken>()).Returns(Project.Create("project", "owner"));
        _factory.TimelineRepository.GetByIdAsync(timeline.Id, Arg.Any<CancellationToken>()).Returns(timeline);
        _factory.RenderJobRepository.GetByIdAsync(render.Id, Arg.Any<CancellationToken>()).Returns(render);

        var response = await _client.PostAsJsonAsync("/api/v1/export", Request(render.Id, timeline.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        await _factory.ExportJobRepository.Received().AddAsync(Arg.Any<ExportJob>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_ShouldReturnExportOrNotFound()
    {
        var job = CreateExport();
        _factory.ExportJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        _factory.ExportJobRepository.GetByIdAsync("missing", Arg.Any<CancellationToken>()).Returns((ExportJob?)null);

        (await _client.GetAsync($"/api/v1/export/{job.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.GetAsync("/api/v1/export/missing")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_ShouldReturnProjectExports()
    {
        var job = CreateExport();
        _factory.ProjectRepository.GetByIdAsync("p1", Arg.Any<CancellationToken>()).Returns(Project.Create("project", "owner"));
        _factory.ExportJobRepository.GetByProjectIdPagedAsync("p1", 1, 20, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ExportJob>(new[] { job }, 1, 1, 20));

        var response = await _client.GetAsync("/api/v1/projects/p1/exports");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain(job.Id);
    }

    [Fact]
    public async Task Cancel_ShouldReturnOk()
    {
        var job = CreateExport();
        _factory.ExportJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var response = await _client.PostAsync($"/api/v1/export/{job.Id}/cancel", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        job.Status.Should().Be(ExportStatus.Cancelled);
    }

    [Fact]
    public async Task Retry_ShouldReturnOkForFailedExport()
    {
        var job = CreateExport();
        job.Start(); job.Fail("simulated");
        _factory.ExportJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var response = await _client.PostAsync($"/api/v1/export/{job.Id}/retry", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ShouldReturnForbiddenForAnotherOwner()
    {
        var job = ExportJob.Create("r", "p1", "t", "another-owner", TimeSpan.Zero, "1920x1080", 30,
            VideoCodec.H264, AudioCodec.AAC, ContainerFormat.MP4);
        _factory.ExportJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var response = await _client.GetAsync($"/api/v1/export/{job.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Endpoint_ShouldReturnUnauthorizedWithoutAuthenticatedPrincipal()
    {
        using var factory = new UnauthenticatedExportFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/export/anything");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static CreateExportRequest Request(string renderId, string timelineId) => new()
    {
        RenderJobId = renderId,
        ProjectId = "p1",
        TimelineId = timelineId,
        VideoCodec = VideoCodec.H264,
        AudioCodec = AudioCodec.AAC,
        Container = ContainerFormat.MP4
    };

    private static ExportJob CreateExport() => ExportJob.Create(
        "render", "p1", "timeline", "owner", TimeSpan.Zero, "1920x1080", 30,
        VideoCodec.H264, AudioCodec.AAC, ContainerFormat.MP4);
}

internal sealed class UnauthenticatedExportFactory : CustomWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Reject";
                options.DefaultChallengeScheme = "Reject";
            }).AddScheme<AuthenticationSchemeOptions, RejectAuthenticationHandler>("Reject", _ => { });
        });
    }
}

internal sealed class RejectAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public RejectAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(AuthenticateResult.Fail("No test identity."));
}
