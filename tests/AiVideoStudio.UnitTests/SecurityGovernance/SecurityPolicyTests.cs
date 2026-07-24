using AiVideoStudio.Domain.Entities.SecurityGovernance;
using Xunit;

namespace AiVideoStudio.UnitTests.SecurityGovernance;

public class SecurityPolicyTests
{
    [Fact]
    public void SecurityPolicy_CreateDefault_SetsExpectedState()
    {
        var policy = SecurityPolicy.CreateDefault("ZeroTrustMaster", "admin-1");

        Assert.NotNull(policy);
        Assert.Equal("ZeroTrustMaster", policy.Name);
        Assert.True(policy.MfaRequiredForAdmins);
        Assert.True(policy.MfaRequiredForHighRisk);
        Assert.Equal(70, policy.MinDeviceTrustScore);
        Assert.Equal(75.0, policy.HighRiskThreshold);
        Assert.Single(policy.DomainEvents);
    }

    [Fact]
    public void SecurityPolicy_UpdatePolicySettings_UpdatesState()
    {
        var policy = SecurityPolicy.CreateDefault();
        policy.UpdatePolicySettings(false, true, 80, 85.0, 3, 45, "admin-2");

        Assert.False(policy.MfaRequiredForAdmins);
        Assert.Equal(80, policy.MinDeviceTrustScore);
        Assert.Equal(85.0, policy.HighRiskThreshold);
        Assert.Equal(3, policy.MaxFailedLoginsBeforeLockout);
        Assert.Equal("admin-2", policy.UpdatedBy);
    }
}
