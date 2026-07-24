namespace AiVideoStudio.Application.Interfaces.OperationsAdmin;

public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string flagName, string? userId = null, string? tenantId = null, CancellationToken cancellationToken = default);
    Task SetFlagAsync(string flagName, bool enabled, string updatedBy, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, bool>> GetAllFlagsAsync(CancellationToken cancellationToken = default);
    void ReloadCache();
}
