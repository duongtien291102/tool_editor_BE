using AiVideoStudio.Domain.Entities.OperationsAdmin;
using Xunit;

namespace AiVideoStudio.UnitTests.OperationsAdmin;

public class PlatformAdministrationTests
{
    [Fact]
    public void PlatformConfiguration_CreateDefault_SetsExpectedProperties()
    {
        var config = PlatformConfiguration.CreateDefault("Production", "admin-1");

        Assert.NotNull(config);
        Assert.Equal("Production", config.Environment);
        Assert.True(config.RetentionDays > 0);
        Assert.True(config.EnforceStrictSecurity);
        Assert.True(config.FeatureFlags["EnableDistributedScheduler"]);
        Assert.Single(config.DomainEvents);
    }

    [Fact]
    public void PlatformConfiguration_UpdateSettings_ModifiesState()
    {
        var config = PlatformConfiguration.CreateDefault("Production", "admin-1");
        config.UpdateSettings(60, 20, 100, "0 * * * *", false, "admin-2");

        Assert.Equal(60, config.RetentionDays);
        Assert.Equal(20, config.MaxConcurrentJobsPerTenant);
        Assert.Equal(100, config.MaxDailyExportsPerUser);
        Assert.Equal("0 * * * *", config.SchedulerCronExpression);
        Assert.False(config.EnforceStrictSecurity);
        Assert.Equal("admin-2", config.UpdatedBy);
    }

    [Fact]
    public void PlatformConfiguration_SetFeatureFlag_UpdatesFlag()
    {
        var config = PlatformConfiguration.CreateDefault("Production", "admin-1");
        config.SetFeatureFlag("TestFlag", true, "admin-1");

        Assert.True(config.FeatureFlags["TestFlag"]);
    }
}
