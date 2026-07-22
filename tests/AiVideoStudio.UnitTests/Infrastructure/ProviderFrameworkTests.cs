using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Features.RenderJobs.DTOs;
using AiVideoStudio.Application.Interfaces.Render;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Infrastructure.Render;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace AiVideoStudio.UnitTests.Infrastructure;

public class RenderProviderRegistryTests
{
    [Fact]
    public void Registry_ShouldResolveRegisteredProvider()
    {
        var provider = ProviderStub(RenderProvider.OpenAI);
        var registry = new RenderProviderRegistry(new[] { provider });

        registry.TryGetProvider(RenderProvider.OpenAI, out var resolved).Should().BeTrue();
        resolved.Should().BeSameAs(provider);
    }

    [Fact]
    public void Register_DuplicateProvider_ShouldFailFast()
    {
        var registry = new RenderProviderRegistry(new[] { ProviderStub(RenderProvider.OpenAI) });

        var act = () => registry.Register(ProviderStub(RenderProvider.OpenAI));

        act.Should().Throw<InvalidOperationException>();
    }

    internal static IRenderProvider ProviderStub(RenderProvider provider)
    {
        var implementation = Substitute.For<IRenderProvider>();
        implementation.Provider.Returns(provider);
        implementation.ProviderName.Returns(provider.ToString());
        implementation.Capabilities.Returns(new HashSet<ProviderCapability>());
        return implementation;
    }
}

public class ProviderFactoryAndSelectorTests
{
    [Fact]
    public void Factory_ShouldReturnPreferredHealthyProvider()
    {
        var openAi = RenderProviderRegistryTests.ProviderStub(RenderProvider.OpenAI);
        var selector = CreateSelector(new[] { openAi }, new MockProviderHealthChecker());
        var factory = new RenderProviderFactory(selector);

        factory.GetProvider(RenderProvider.OpenAI).Should().BeSameAs(openAi);
    }

    [Fact]
    public void Selector_ShouldSkipUnhealthyPreferredProvider()
    {
        var openAi = RenderProviderRegistryTests.ProviderStub(RenderProvider.OpenAI);
        var runway = RenderProviderRegistryTests.ProviderStub(RenderProvider.Runway);
        var health = new MockProviderHealthChecker();
        health.SetHealth(RenderProvider.OpenAI, false);
        var selector = CreateSelector(new[] { openAi, runway }, health);

        selector.Select(RenderProvider.OpenAI).Should().BeSameAs(runway);
    }

    [Fact]
    public void Selector_WithNoAvailableProvider_ShouldThrow()
    {
        var openAi = RenderProviderRegistryTests.ProviderStub(RenderProvider.OpenAI);
        var health = new MockProviderHealthChecker();
        health.SetHealth(RenderProvider.OpenAI, false);
        var selector = CreateSelector(new[] { openAi }, health);

        var act = () => selector.Select(RenderProvider.OpenAI);

        act.Should().Throw<ProviderUnavailableException>();
    }

    private static FirstAvailableProviderSelector CreateSelector(
        IEnumerable<IRenderProvider> providers,
        IProviderHealthChecker health) =>
        new(
            new RenderProviderRegistry(providers),
            health,
            new TestOptionsMonitor<ProviderOptions>(new ProviderOptions()));
}

public class ProviderHealthCheckerTests
{
    [Fact]
    public void HealthChecker_ShouldBeHealthyByDefault_AndAllowStateChanges()
    {
        var checker = new MockProviderHealthChecker();

        checker.IsHealthy(RenderProvider.Kling).Should().BeTrue();
        checker.SetHealth(RenderProvider.Kling, false);
        checker.IsHealthy(RenderProvider.Kling).Should().BeFalse();
    }
}

public class MemoryApiKeyProviderTests
{
    [Fact]
    public void ApiKeyProvider_ShouldStoreAndRemoveKeysInMemory()
    {
        var provider = new MemoryApiKeyProvider(
            new TestOptionsMonitor<ProviderOptions>(new ProviderOptions()));

        provider.SetApiKey(RenderProvider.Veo, "runtime-secret");
        provider.GetApiKey(RenderProvider.Veo).Should().Be("runtime-secret");
        provider.RemoveApiKey(RenderProvider.Veo).Should().BeTrue();
        provider.GetApiKey(RenderProvider.Veo).Should().BeNull();
    }

    [Fact]
    public void ApiKeyProvider_ShouldReadConfiguredKeyThroughOptions()
    {
        var options = new TestOptionsMonitor<ProviderOptions>(new ProviderOptions());
        options.Set(RenderProvider.OpenAI.ToString(), new ProviderOptions { ApiKey = "configured-secret" });
        var provider = new MemoryApiKeyProvider(options);

        provider.GetApiKey(RenderProvider.OpenAI).Should().Be("configured-secret");
    }
}

public class AbstractRenderProviderTests
{
    [Fact]
    public async Task RenderAsync_ShouldRetryAndReturnSuccess()
    {
        var provider = new TestRenderProvider(failuresBeforeSuccess: 1, retry: 1);
        var job = CreateJob(RenderJobType.GenerateImage);

        var result = await provider.RenderAsync(job);

        result.IsSuccess.Should().BeTrue();
        provider.Attempts.Should().Be(2);
    }

    [Fact]
    public async Task RenderAsync_ShouldMapExceptions()
    {
        var provider = new TestRenderProvider(failuresBeforeSuccess: int.MaxValue, retry: 0);

        var result = await provider.RenderAsync(CreateJob(RenderJobType.GenerateImage));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PROVIDER_CONNECTION_ERROR");
    }

    [Fact]
    public async Task RenderAsync_ShouldHonorCancellation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var provider = new TestRenderProvider(0, 0);

        var result = await provider.RenderAsync(CreateJob(RenderJobType.GenerateImage), source.Token);

        result.ErrorCode.Should().Be("CANCELLED");
    }

    [Fact]
    public async Task RenderAsync_ShouldRejectUnsupportedCapability()
    {
        var provider = new TestRenderProvider(0, 0);

        var result = await provider.RenderAsync(CreateJob(RenderJobType.GenerateVoice));

        result.ErrorCode.Should().Be("CAPABILITY_NOT_SUPPORTED");
        provider.Attempts.Should().Be(0);
    }

    private static RenderJob CreateJob(RenderJobType type) =>
        RenderJob.Create("project", "owner", type, RenderProvider.Custom);

    private sealed class TestRenderProvider : AbstractRenderProvider
    {
        private static readonly IReadOnlySet<ProviderCapability> Supported =
            new HashSet<ProviderCapability> { ProviderCapability.GenerateImage };
        private readonly int _failuresBeforeSuccess;

        public TestRenderProvider(int failuresBeforeSuccess, int retry)
            : this(
                failuresBeforeSuccess,
                new TestOptionsMonitor<ProviderOptions>(new ProviderOptions { Retry = retry, Timeout = 5 }))
        {
        }

        private TestRenderProvider(
            int failuresBeforeSuccess,
            TestOptionsMonitor<ProviderOptions> options)
            : base(
                NullLogger<TestRenderProvider>.Instance,
                options,
                new MemoryApiKeyProvider(options))
        {
            _failuresBeforeSuccess = failuresBeforeSuccess;
        }

        public int Attempts { get; private set; }
        public override RenderProvider Provider => RenderProvider.Custom;
        public override IReadOnlySet<ProviderCapability> Capabilities => Supported;

        protected override Task<string?> RenderInternalAsync(RenderJob job, CancellationToken token)
        {
            Attempts++;
            token.ThrowIfCancellationRequested();
            if (Attempts <= _failuresBeforeSuccess)
            {
                throw new HttpRequestException("simulated connection failure");
            }

            return Task.FromResult<string?>("{}");
        }
    }
}

public class MockAiProviderTests
{
    [Fact]
    public async Task MockProviders_ShouldDeclareCapabilities_AndReturnSimulatedResults()
    {
        var options = new TestOptionsMonitor<ProviderOptions>(new ProviderOptions());
        var keys = new MemoryApiKeyProvider(options);
        AbstractRenderProvider[] providers =
        {
            new MockOpenAIProvider(NullLogger<MockOpenAIProvider>.Instance, options, keys),
            new MockRunwayProvider(NullLogger<MockRunwayProvider>.Instance, options, keys),
            new MockKlingProvider(NullLogger<MockKlingProvider>.Instance, options, keys),
            new MockVeoProvider(NullLogger<MockVeoProvider>.Instance, options, keys),
            new MockElevenLabsProvider(NullLogger<MockElevenLabsProvider>.Instance, options, keys),
            new MockStableVideoProvider(NullLogger<MockStableVideoProvider>.Instance, options, keys)
        };

        foreach (var provider in providers)
        {
            provider.Capabilities.Should().NotBeEmpty();
            var jobType = provider.Provider == RenderProvider.ElevenLabs
                ? RenderJobType.GenerateVoice
                : provider.Provider == RenderProvider.OpenAI
                    ? RenderJobType.GenerateImage
                    : RenderJobType.GenerateVideo;
            var job = RenderJob.Create("project", "owner", jobType, provider.Provider);
            var result = await provider.RenderAsync(job);
            result.IsSuccess.Should().BeTrue(provider.Provider.ToString());
            result.OutputPayload.Should().Contain(provider.Provider.ToString());
        }
    }
}

internal sealed class TestOptionsMonitor<T> : IOptionsMonitor<T> where T : class
{
    private readonly Dictionary<string, T> _named = new();

    public TestOptionsMonitor(T currentValue)
    {
        CurrentValue = currentValue;
    }

    public T CurrentValue { get; }
    public T Get(string? name) => name is not null && _named.TryGetValue(name, out var value) ? value : CurrentValue;
    public IDisposable? OnChange(Action<T, string?> listener) => null;
    public void Set(string name, T value) => _named[name] = value;
}
