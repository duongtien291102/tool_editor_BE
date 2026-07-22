using System.Collections.Concurrent;
using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Interfaces.Render;
using AiVideoStudio.Domain.Enums;
using Microsoft.Extensions.Options;

namespace AiVideoStudio.Infrastructure.Render;

public sealed class MemoryApiKeyProvider : IApiKeyProvider
{
    private readonly ConcurrentDictionary<RenderProvider, string> _keys = new();
    private readonly IOptionsMonitor<ProviderOptions> _options;

    public MemoryApiKeyProvider(IOptionsMonitor<ProviderOptions> options)
    {
        _options = options;
    }

    public string? GetApiKey(RenderProvider provider)
    {
        if (_keys.TryGetValue(provider, out var key))
        {
            return key;
        }

        var configuredKey = _options.Get(provider.ToString()).ApiKey;
        return string.IsNullOrWhiteSpace(configuredKey) ? null : configuredKey;
    }

    public void SetApiKey(RenderProvider provider, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key cannot be empty.", nameof(apiKey));
        }

        _keys[provider] = apiKey;
    }

    public bool RemoveApiKey(RenderProvider provider) => _keys.TryRemove(provider, out _);
}
