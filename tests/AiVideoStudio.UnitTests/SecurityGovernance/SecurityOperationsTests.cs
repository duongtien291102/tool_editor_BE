using AiVideoStudio.Application.Features.SecurityGovernance.Services;
using AiVideoStudio.Application.Interfaces.SecurityGovernance;
using AiVideoStudio.Domain.Entities.SecurityGovernance;
using AiVideoStudio.Domain.Interfaces.SecurityGovernance;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace AiVideoStudio.UnitTests.SecurityGovernance;

public class ThreatDetectionTests
{
    [Fact]
    public async Task ThreatDetection_FailedLogins_TriggersIncident()
    {
        var repo = Substitute.For<ISecurityRepository>();
        UserSecurityProfile? profile = null;
        repo.GetUserProfileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(profile));
        repo.SaveUserProfileAsync(Arg.Do<UserSecurityProfile>(p => profile = p), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var service = new ThreatDetectionService(repo, NullLogger<ThreatDetectionService>.Instance);

        for (int i = 0; i < 5; i++)
        {
            await service.AnalyzeActivityAsync("user-100", "192.168.1.50", "Mozilla", "LOGIN_FAILED");
        }

        await repo.Received(1).SaveSecurityIncidentAsync(Arg.Is<SecurityIncidentRecord>(inc => inc.ThreatType == "BruteForce"), Arg.Any<CancellationToken>());
    }
}

public class RiskEngineTests
{
    [Fact]
    public async Task RiskEngine_CalculateRisk_ReturnsHighRiskForUntrustedDevice()
    {
        var repo = Substitute.For<ISecurityRepository>();
        repo.GetUserProfileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((UserSecurityProfile?)null);
        repo.GetDeviceByFingerprintAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((TrustedDevice?)null);

        var engine = new RiskEngine(repo);
        var result = await engine.CalculateRiskAsync("user-1", "10.0.0.1", "Chrome", "fp-unknown");

        Assert.NotNull(result);
        Assert.True(result.OverallRiskScore >= 25);
        Assert.NotEmpty(result.RiskFactors);
    }
}

public class ComplianceTests
{
    [Fact]
    public async Task ComplianceService_GenerateReport_ProducesSOC2Report()
    {
        var repo = Substitute.For<ISecurityRepository>();
        var service = new ComplianceService(repo, NullLogger<ComplianceService>.Instance);

        var report = await service.GenerateReportAsync("SOC2", "auditor-1");

        Assert.NotNull(report);
        Assert.Equal("SOC2", report.FrameworkType);
        Assert.NotEmpty(report.Findings);
        await repo.Received(1).SaveComplianceReportAsync(Arg.Any<ComplianceReport>(), Arg.Any<CancellationToken>());
    }
}

public class SecretsManagerTests
{
    [Fact]
    public async Task SecretsManager_RotateSecret_IncrementsVersion()
    {
        var manager = new SecretsManager(NullLogger<SecretsManager>.Instance);
        var meta1 = await manager.RotateSecretAsync("TestSecretKey", "admin-1");

        Assert.NotNull(meta1);
        Assert.Equal("TestSecretKey", meta1.KeyName);
        Assert.Equal(1, meta1.Version);

        var meta2 = await manager.RotateSecretAsync("TestSecretKey", "admin-1");
        Assert.Equal(2, meta2.Version);
    }
}

public class SecurityOperationsTests
{
    [Fact]
    public async Task PolicyEngine_EvaluateAsync_EnforcesRBAC()
    {
        var repo = Substitute.For<ISecurityRepository>();
        var policy = SecurityPolicy.CreateDefault();
        repo.GetActiveSecurityPolicyAsync(Arg.Any<CancellationToken>()).Returns(policy);

        var engine = new PolicyEngine(repo, NullLogger<PolicyEngine>.Instance);

        var adminContext = new AuthorizationContext("admin-1", new List<string> { "Admin" }, new List<string>(), "127.0.0.1", "Mozilla", "t1", "project", "read");
        bool adminAllowed = await engine.EvaluateAsync(adminContext);
        Assert.True(adminAllowed);

        var userContext = new AuthorizationContext("user-1", new List<string> { "User" }, new List<string>(), "127.0.0.1", "Mozilla", "t1", "admin", "delete");
        bool userAllowed = await engine.EvaluateAsync(userContext);
        Assert.False(userAllowed);
    }
}
