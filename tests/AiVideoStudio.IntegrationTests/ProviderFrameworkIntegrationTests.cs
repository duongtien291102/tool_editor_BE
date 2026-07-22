using AiVideoStudio.Application.Features.RenderJobs.DTOs;
using AiVideoStudio.Application.Interfaces.Render;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Infrastructure.Render;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace AiVideoStudio.IntegrationTests;

public class ProviderFrameworkIntegrationTests
{
    [Fact]
    public void DependencyInjection_ShouldResolveFactoryRegistryAndAllMockProviders()
    {
        using var factory = new CustomWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var registry = scope.ServiceProvider.GetRequiredService<IRenderProviderRegistry>();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IRenderProviderFactory>();

        registry.GetProviders().Should().HaveCount(7);
        foreach (var provider in new[]
                 {
                     RenderProvider.Internal, RenderProvider.OpenAI, RenderProvider.Runway,
                     RenderProvider.Kling, RenderProvider.Veo, RenderProvider.ElevenLabs,
                     RenderProvider.StableVideo
                 })
        {
            providerFactory.GetProvider(provider).Provider.Should().Be(provider);
        }
    }

    [Fact]
    public void Factory_ShouldSkipProviderMarkedUnhealthyByHealthChecker()
    {
        using var factory = new CustomWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var health = scope.ServiceProvider.GetRequiredService<MockProviderHealthChecker>();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IRenderProviderFactory>();

        health.SetHealth(RenderProvider.OpenAI, false);

        providerFactory.GetProvider(RenderProvider.OpenAI).Provider.Should().NotBe(RenderProvider.OpenAI);
    }

    [Fact]
    public async Task RenderWorker_ShouldProcessJobsWithMultipleProviders()
    {
        using var factory = new CustomWebApplicationFactory();
        _ = factory.CreateClient();
        var queue = factory.Services.GetRequiredService<IRenderQueue>();
        var jobs = new[]
        {
            CreateQueuedJob(RenderJobType.GenerateImage, RenderProvider.OpenAI),
            CreateQueuedJob(RenderJobType.GenerateVideo, RenderProvider.Runway)
        };

        factory.RenderJobRepository
            .GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => jobs.SingleOrDefault(job => job.Id == call.ArgAt<string>(0)));

        foreach (var job in jobs)
        {
            await queue.EnqueueAsync(new QueueItem(job.Id, job.Priority, job.CreatedAt));
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (jobs.Any(job => job.Status != RenderJobStatus.Completed))
        {
            await Task.Delay(20, timeout.Token);
        }

        jobs.Should().OnlyContain(job => job.Status == RenderJobStatus.Completed);
        await factory.RenderJobRepository.Received(4)
            .UpdateAsync(Arg.Any<RenderJob>(), Arg.Any<CancellationToken>());
    }

    private static RenderJob CreateQueuedJob(RenderJobType type, RenderProvider provider)
    {
        var job = RenderJob.Create("project", "owner", type, provider);
        job.Queue();
        return job;
    }
}
