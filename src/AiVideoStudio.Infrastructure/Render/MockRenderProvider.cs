using System.Text.Json;
using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Interfaces.Render;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiVideoStudio.Infrastructure.Render;

public sealed class MockRenderProvider : AbstractRenderProvider
{
    private static readonly IReadOnlySet<ProviderCapability> SupportedCapabilities =
        new HashSet<ProviderCapability>(Enum.GetValues<ProviderCapability>());

    public MockRenderProvider(
        ILogger<MockRenderProvider> logger,
        IOptionsMonitor<ProviderOptions> options,
        IApiKeyProvider apiKeyProvider)
        : base(logger, options, apiKeyProvider) { }

    public override RenderProvider Provider => RenderProvider.Internal;
    public override IReadOnlySet<ProviderCapability> Capabilities => SupportedCapabilities;

    protected override Task<string?> RenderInternalAsync(RenderJob job, CancellationToken cancellationToken) =>
        MockRenderSimulation.RenderAsync(Provider, job, cancellationToken);
}

internal static class MockRenderSimulation
{
    public static async Task<string?> RenderAsync(
        RenderProvider provider,
        RenderJob job,
        CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(25), cancellationToken);
        return JsonSerializer.Serialize(new
        {
            jobId = job.Id,
            provider = provider.ToString(),
            status = "Success",
            mockData = "Simulated render result.",
            generatedAt = DateTimeOffset.UtcNow
        });
    }
}
