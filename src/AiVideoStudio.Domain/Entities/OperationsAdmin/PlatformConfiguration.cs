using AiVideoStudio.Domain.Base;
using AiVideoStudio.Domain.Events;

namespace AiVideoStudio.Domain.Entities.OperationsAdmin;

public sealed class PlatformConfigurationChangedEvent : IDomainEvent
{
    public string ConfigurationId { get; }
    public string UpdatedBy { get; }
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;

    public PlatformConfigurationChangedEvent(string configurationId, string updatedBy)
    {
        ConfigurationId = configurationId;
        UpdatedBy = updatedBy;
    }
}

public sealed class PlatformConfiguration : BaseEntity
{
    public string Environment { get; private set; } = "Production";
    public Dictionary<string, bool> FeatureFlags { get; private set; } = new();
    public Dictionary<string, string> AiDefaults { get; private set; } = new();
    public Dictionary<string, string> ExportDefaults { get; private set; } = new();
    public int RetentionDays { get; private set; } = 30;
    public string StorageBasePath { get; private set; } = "./storage";
    public bool EnforceStrictSecurity { get; private set; } = true;
    public int MaxConcurrentJobsPerTenant { get; private set; } = 10;
    public int MaxDailyExportsPerUser { get; private set; } = 50;
    public string SchedulerCronExpression { get; private set; } = "*/5 * * * *";

    private PlatformConfiguration() { }

    public static PlatformConfiguration CreateDefault(string environment = "Production", string createdBy = "system")
    {
        var config = new PlatformConfiguration
        {
            Environment = string.IsNullOrWhiteSpace(environment) ? "Production" : environment,
            RetentionDays = 30,
            StorageBasePath = "./storage",
            EnforceStrictSecurity = true,
            MaxConcurrentJobsPerTenant = 10,
            MaxDailyExportsPerUser = 50,
            SchedulerCronExpression = "*/5 * * * *",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = createdBy
        };

        config.FeatureFlags["EnableDistributedScheduler"] = true;
        config.FeatureFlags["EnableHardwareAcceleration"] = true;
        config.FeatureFlags["EnableRealtimeStreaming"] = true;
        config.FeatureFlags["EnableBrowserPoolStealth"] = true;

        config.AiDefaults["DefaultVideoModel"] = "Runway-Gen3";
        config.AiDefaults["DefaultAudioModel"] = "ElevenLabs-v2";

        config.ExportDefaults["DefaultResolution"] = "1080p";
        config.ExportDefaults["DefaultContainer"] = "MP4";

        config.AddDomainEvent(new PlatformConfigurationChangedEvent(config.Id, createdBy));
        return config;
    }

    public void UpdateSettings(
        int retentionDays,
        int maxConcurrentJobs,
        int maxDailyExports,
        string schedulerCron,
        bool enforceSecurity,
        string updatedBy)
    {
        if (retentionDays <= 0) throw new ArgumentOutOfRangeException(nameof(retentionDays));
        if (maxConcurrentJobs <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrentJobs));
        if (maxDailyExports <= 0) throw new ArgumentOutOfRangeException(nameof(maxDailyExports));

        RetentionDays = retentionDays;
        MaxConcurrentJobsPerTenant = maxConcurrentJobs;
        MaxDailyExportsPerUser = maxDailyExports;
        SchedulerCronExpression = string.IsNullOrWhiteSpace(schedulerCron) ? SchedulerCronExpression : schedulerCron;
        EnforceStrictSecurity = enforceSecurity;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;

        AddDomainEvent(new PlatformConfigurationChangedEvent(Id, updatedBy));
    }

    public void SetFeatureFlag(string flagName, bool enabled, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(flagName)) throw new ArgumentException("Flag name is required.", nameof(flagName));
        FeatureFlags[flagName.Trim()] = enabled;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
        AddDomainEvent(new PlatformConfigurationChangedEvent(Id, updatedBy));
    }
}
