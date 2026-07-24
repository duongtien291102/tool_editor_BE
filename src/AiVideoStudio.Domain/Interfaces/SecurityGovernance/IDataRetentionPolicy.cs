namespace AiVideoStudio.Domain.Interfaces.SecurityGovernance;

public interface IDataRetentionPolicy
{
    int RetentionDays { get; }
    bool AutoPurgeExpiredData { get; }
    bool ColdStorageArchivalEnabled { get; }
    Task EnforceRetentionPolicyAsync(CancellationToken cancellationToken = default);
}
