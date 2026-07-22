using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Interfaces.Render;
using AiVideoStudio.Domain.Enums;
using Microsoft.Extensions.Options;

namespace AiVideoStudio.Infrastructure.Render;

public sealed class FirstAvailableProviderSelector : IProviderSelector
{
    private readonly IRenderProviderRegistry _registry;
    private readonly IProviderHealthChecker _healthChecker;
    private readonly IOptionsMonitor<ProviderOptions> _options;

    public FirstAvailableProviderSelector(
        IRenderProviderRegistry registry,
        IProviderHealthChecker healthChecker,
        IOptionsMonitor<ProviderOptions> options)
    {
        _registry = registry;
        _healthChecker = healthChecker;
        _options = options;
    }

    public IRenderProvider Select(RenderProvider preferredProvider)
    {
        if (_registry.TryGetProvider(preferredProvider, out var preferred) && IsAvailable(preferred!))
        {
            return preferred!;
        }

        var fallback = _registry.GetProviders().FirstOrDefault(IsAvailable);
        return fallback ?? throw new ProviderUnavailableException(preferredProvider);
    }

    private bool IsAvailable(IRenderProvider provider) =>
        _options.Get(provider.Provider.ToString()).Enabled &&
        _healthChecker.IsHealthy(provider.Provider);
}
