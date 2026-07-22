using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Interfaces.Render;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiVideoStudio.Infrastructure.Render;

public sealed class MockOpenAIProvider : AbstractRenderProvider
{
    private static readonly IReadOnlySet<ProviderCapability> Supported = new HashSet<ProviderCapability>
    {
        ProviderCapability.GenerateImage, ProviderCapability.GenerateSubtitle,
        ProviderCapability.Inpainting, ProviderCapability.Outpainting,
        ProviderCapability.ImageEditing
    };

    public MockOpenAIProvider(ILogger<MockOpenAIProvider> logger, IOptionsMonitor<ProviderOptions> options, IApiKeyProvider keys)
        : base(logger, options, keys) { }
    public override RenderProvider Provider => RenderProvider.OpenAI;
    public override IReadOnlySet<ProviderCapability> Capabilities => Supported;
    protected override Task<string?> RenderInternalAsync(RenderJob job, CancellationToken token) => MockRenderSimulation.RenderAsync(Provider, job, token);
}

public sealed class MockRunwayProvider : AbstractRenderProvider
{
    private static readonly IReadOnlySet<ProviderCapability> Supported = new HashSet<ProviderCapability>
    {
        ProviderCapability.GenerateVideo, ProviderCapability.ImageEditing,
        ProviderCapability.Inpainting, ProviderCapability.Upscale
    };

    public MockRunwayProvider(ILogger<MockRunwayProvider> logger, IOptionsMonitor<ProviderOptions> options, IApiKeyProvider keys)
        : base(logger, options, keys) { }
    public override RenderProvider Provider => RenderProvider.Runway;
    public override IReadOnlySet<ProviderCapability> Capabilities => Supported;
    protected override Task<string?> RenderInternalAsync(RenderJob job, CancellationToken token) => MockRenderSimulation.RenderAsync(Provider, job, token);
}

public sealed class MockKlingProvider : AbstractRenderProvider
{
    private static readonly IReadOnlySet<ProviderCapability> Supported = new HashSet<ProviderCapability>
    {
        ProviderCapability.GenerateVideo, ProviderCapability.ImageEditing
    };

    public MockKlingProvider(ILogger<MockKlingProvider> logger, IOptionsMonitor<ProviderOptions> options, IApiKeyProvider keys)
        : base(logger, options, keys) { }
    public override RenderProvider Provider => RenderProvider.Kling;
    public override IReadOnlySet<ProviderCapability> Capabilities => Supported;
    protected override Task<string?> RenderInternalAsync(RenderJob job, CancellationToken token) => MockRenderSimulation.RenderAsync(Provider, job, token);
}

public sealed class MockVeoProvider : AbstractRenderProvider
{
    private static readonly IReadOnlySet<ProviderCapability> Supported = new HashSet<ProviderCapability>
    {
        ProviderCapability.GenerateVideo
    };

    public MockVeoProvider(ILogger<MockVeoProvider> logger, IOptionsMonitor<ProviderOptions> options, IApiKeyProvider keys)
        : base(logger, options, keys) { }
    public override RenderProvider Provider => RenderProvider.Veo;
    public override IReadOnlySet<ProviderCapability> Capabilities => Supported;
    protected override Task<string?> RenderInternalAsync(RenderJob job, CancellationToken token) => MockRenderSimulation.RenderAsync(Provider, job, token);
}

public sealed class MockElevenLabsProvider : AbstractRenderProvider
{
    private static readonly IReadOnlySet<ProviderCapability> Supported = new HashSet<ProviderCapability>
    {
        ProviderCapability.GenerateVoice
    };

    public MockElevenLabsProvider(ILogger<MockElevenLabsProvider> logger, IOptionsMonitor<ProviderOptions> options, IApiKeyProvider keys)
        : base(logger, options, keys) { }
    public override RenderProvider Provider => RenderProvider.ElevenLabs;
    public override IReadOnlySet<ProviderCapability> Capabilities => Supported;
    protected override Task<string?> RenderInternalAsync(RenderJob job, CancellationToken token) => MockRenderSimulation.RenderAsync(Provider, job, token);
}

public sealed class MockStableVideoProvider : AbstractRenderProvider
{
    private static readonly IReadOnlySet<ProviderCapability> Supported = new HashSet<ProviderCapability>
    {
        ProviderCapability.GenerateVideo, ProviderCapability.Upscale
    };

    public MockStableVideoProvider(ILogger<MockStableVideoProvider> logger, IOptionsMonitor<ProviderOptions> options, IApiKeyProvider keys)
        : base(logger, options, keys) { }
    public override RenderProvider Provider => RenderProvider.StableVideo;
    public override IReadOnlySet<ProviderCapability> Capabilities => Supported;
    protected override Task<string?> RenderInternalAsync(RenderJob job, CancellationToken token) => MockRenderSimulation.RenderAsync(Provider, job, token);
}
