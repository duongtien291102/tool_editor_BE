using AiVideoStudio.Domain.Base;
using AiVideoStudio.Domain.Events;

namespace AiVideoStudio.Domain.Entities.SecurityGovernance;

public sealed class SecurityPolicyUpdatedEvent : IDomainEvent
{
    public string PolicyId { get; }
    public string UpdatedBy { get; }
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;

    public SecurityPolicyUpdatedEvent(string policyId, string updatedBy)
    {
        PolicyId = policyId;
        UpdatedBy = updatedBy;
    }
}

public sealed class SecurityPolicy : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool MfaRequiredForAdmins { get; private set; } = true;
    public bool MfaRequiredForHighRisk { get; private set; } = true;
    public int MinDeviceTrustScore { get; private set; } = 70;
    public double HighRiskThreshold { get; private set; } = 75.0;
    public int MaxFailedLoginsBeforeLockout { get; private set; } = 5;
    public int LockoutDurationMinutes { get; private set; } = 30;
    public Dictionary<string, List<string>> RbacRules { get; private set; } = new();
    public List<AbacRule> AbacRules { get; private set; } = new();

    private SecurityPolicy() { }

    public static SecurityPolicy CreateDefault(string name = "DefaultZeroTrustPolicy", string createdBy = "system")
    {
        var policy = new SecurityPolicy
        {
            Name = name,
            Description = "Default Platform Zero Trust Security Policy",
            MfaRequiredForAdmins = true,
            MfaRequiredForHighRisk = true,
            MinDeviceTrustScore = 70,
            HighRiskThreshold = 75.0,
            MaxFailedLoginsBeforeLockout = 5,
            LockoutDurationMinutes = 30,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = createdBy
        };

        policy.RbacRules["Admin"] = new List<string> { "*:*", "admin:*" };
        policy.RbacRules["User"] = new List<string> { "project:read", "project:write", "render:create", "export:create" };

        policy.AbacRules.Add(new AbacRule("IpRestriction", "ClientIp", "NotIn", "BlacklistedIps", "Deny"));
        policy.AbacRules.Add(new AbacRule("BusinessHoursOnlyForExport", "TimeOfDay", "Between", "08:00-22:00", "Allow"));

        policy.AddDomainEvent(new SecurityPolicyUpdatedEvent(policy.Id, createdBy));
        return policy;
    }

    public void UpdatePolicySettings(
        bool mfaAdmins,
        bool mfaHighRisk,
        int minDeviceTrust,
        double riskThreshold,
        int maxFailedLogins,
        int lockoutMinutes,
        string updatedBy)
    {
        MfaRequiredForAdmins = mfaAdmins;
        MfaRequiredForHighRisk = mfaHighRisk;
        MinDeviceTrustScore = minDeviceTrust;
        HighRiskThreshold = riskThreshold;
        MaxFailedLoginsBeforeLockout = maxFailedLogins;
        LockoutDurationMinutes = lockoutMinutes;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;

        AddDomainEvent(new SecurityPolicyUpdatedEvent(Id, updatedBy));
    }
}

public sealed record AbacRule(string RuleName, string Attribute, string Operator, string ExpectedValue, string Effect);
