using AiVideoStudio.Application.Interfaces.Render;
using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Infrastructure.Render;

public sealed class RenderProviderRegistry : IRenderProviderRegistry
{
    private readonly Dictionary<RenderProvider, IRenderProvider> _providers = new();
    private readonly object _sync = new();

    public RenderProviderRegistry(IEnumerable<IRenderProvider> providers)
    {
        foreach (var provider in providers)
        {
            Register(provider);
        }
    }

    public void Register(IRenderProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        lock (_sync)
        {
            if (!_providers.TryAdd(provider.Provider, provider))
            {
                throw new InvalidOperationException(
                    $"A render provider is already registered for '{provider.Provider}'.");
            }
        }
    }

    public bool TryGetProvider(RenderProvider provider, out IRenderProvider? implementation)
    {
        lock (_sync)
        {
            return _providers.TryGetValue(provider, out implementation);
        }
    }

    public IReadOnlyCollection<IRenderProvider> GetProviders()
    {
        lock (_sync)
        {
            return _providers.Values.ToArray();
        }
    }
}
