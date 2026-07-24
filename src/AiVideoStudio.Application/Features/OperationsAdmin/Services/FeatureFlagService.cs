using System.Collections.Concurrent;
using AiVideoStudio.Application.Interfaces.OperationsAdmin;
using AiVideoStudio.Domain.Interfaces.OperationsAdmin;
using Microsoft.Extensions.Logging;

namespace AiVideoStudio.Application.Features.OperationsAdmin.Services;

public sealed class FeatureFlagService : IFeatureFlagService
{
    private readonly IPlatformAdministrationRepository _repository;
    private readonly ILogger<FeatureFlagService> _logger;
    private readonly ConcurrentDictionary<string, bool> _cache = new();
    private DateTimeOffset _lastCacheUpdate = DateTimeOffset.MinValue;

    public FeatureFlagService(
        IPlatformAdministrationRepository repository,
        ILogger<FeatureFlagService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> IsEnabledAsync(string flagName, string? userId = null, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(flagName)) return false;

        await EnsureCacheLoadedAsync(cancellationToken);

        if (_cache.TryGetValue(flagName.Trim(), out var enabled))
        {
            // Support percentage rollout or tenant targeting logic if applicable
            return enabled;
        }

        return false;
    }

    public async Task SetFlagAsync(string flagName, bool enabled, string updatedBy, CancellationToken cancellationToken = default)
    {
        var config = await _repository.GetConfigurationAsync(cancellationToken) ?? Domain.Entities.OperationsAdmin.PlatformConfiguration.CreateDefault();
        config.SetFeatureFlag(flagName, enabled, updatedBy);
        await _repository.SaveConfigurationAsync(config, cancellationToken);
        _cache[flagName.Trim()] = enabled;
        _logger.LogInformation("Feature flag '{Flag}' set to {Enabled} by {UpdatedBy}", flagName, enabled, updatedBy);
    }

    public async Task<IReadOnlyDictionary<string, bool>> GetAllFlagsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureCacheLoadedAsync(cancellationToken);
        return _cache.ToDictionary(k => k.Key, v => v.Value);
    }

    public void ReloadCache()
    {
        _lastCacheUpdate = DateTimeOffset.MinValue;
    }

    private async Task EnsureCacheLoadedAsync(CancellationToken cancellationToken)
    {
        if (DateTimeOffset.UtcNow - _lastCacheUpdate < TimeSpan.FromSeconds(30) && !_cache.IsEmpty) return;

        try
        {
            var config = await _repository.GetConfigurationAsync(cancellationToken);
            if (config != null)
            {
                foreach (var (key, value) in config.FeatureFlags)
                {
                    _cache[key] = value;
                }
                _lastCacheUpdate = DateTimeOffset.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load feature flags from repository.");
        }
    }
}
