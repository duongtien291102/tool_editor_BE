using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Entities.SecurityGovernance;

public sealed class ApiRateLimitPolicy : BaseEntity
{
    public string EndpointPath { get; private set; } = string.Empty;
    public int BurstCapacity { get; private set; } = 100;
    public int SustainedRatePerMinute { get; private set; } = 600;
    public int WindowSeconds { get; private set; } = 60;
    public bool IsEnabled { get; private set; } = true;

    private ApiRateLimitPolicy() { }

    public static ApiRateLimitPolicy Create(
        string endpointPath,
        int burstCapacity,
        int sustainedRatePerMinute,
        int windowSeconds = 60,
        string createdBy = "system")
    {
        return new ApiRateLimitPolicy
        {
            EndpointPath = endpointPath,
            BurstCapacity = burstCapacity,
            SustainedRatePerMinute = sustainedRatePerMinute,
            WindowSeconds = windowSeconds,
            IsEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = createdBy
        };
    }

    public void UpdatePolicy(int burst, int sustained, int window, bool enabled, string updatedBy)
    {
        BurstCapacity = burst;
        SustainedRatePerMinute = sustained;
        WindowSeconds = window;
        IsEnabled = enabled;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
