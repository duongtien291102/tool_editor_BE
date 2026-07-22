using System.Collections.Concurrent;
using AiVideoStudio.Application.Interfaces.Render;
using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Infrastructure.Render;

public sealed class MockProviderHealthChecker : IProviderHealthChecker
{
    private readonly ConcurrentDictionary<RenderProvider, bool> _health = new();

    public bool IsHealthy(RenderProvider provider) =>
        !_health.TryGetValue(provider, out var healthy) || healthy;

    public void SetHealth(RenderProvider provider, bool healthy) =>
        _health[provider] = healthy;
}
