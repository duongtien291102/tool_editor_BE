using AiVideoStudio.Application.Features.SecurityGovernance.Services;
using AiVideoStudio.Domain.Entities.SecurityGovernance;
using AiVideoStudio.Domain.Interfaces.SecurityGovernance;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace AiVideoStudio.IntegrationTests.SecurityGovernance;

public class SecurityIntegrationTests
{
    [Fact]
    public async Task SecurityGovernance_EndToEnd_Flow_Succeeds()
    {
        var repo = Substitute.For<ISecurityRepository>();
        SecurityPolicy? activePolicy = null;
        var incidents = new List<SecurityIncidentRecord>();
        var devices = new List<TrustedDevice>();

        repo.GetActiveSecurityPolicyAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(activePolicy));
        repo.SaveSecurityPolicyAsync(Arg.Do<SecurityPolicy>(p => activePolicy = p), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        repo.SaveSecurityIncidentAsync(Arg.Do<SecurityIncidentRecord>(i => incidents.Add(i)), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        repo.GetSecurityIncidentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult<IReadOnlyList<SecurityIncidentRecord>>(incidents));

        repo.SaveTrustedDeviceAsync(Arg.Do<TrustedDevice>(d => devices.Add(d)), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        repo.GetDeviceByFingerprintAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => devices.FirstOrDefault(d => d.UserId == (string)callInfo[0] && d.DeviceFingerprint == (string)callInfo[1]));

        var secretsManager = new SecretsManager(NullLogger<SecretsManager>.Instance);
        var securityService = new SecurityService(repo, secretsManager, NullLogger<SecurityService>.Instance);
        var threatDetection = new ThreatDetectionService(repo, NullLogger<ThreatDetectionService>.Instance);
        var complianceService = new ComplianceService(repo, NullLogger<ComplianceService>.Instance);
        var riskEngine = new RiskEngine(repo);

        // 1. Get & Update Security Policy
        var policy = await securityService.GetSecurityPolicyAsync();
        Assert.NotNull(policy);
        await securityService.UpdateSecurityPolicyAsync(true, true, 75, 80.0, 5, 30, "integration-tester");

        // 2. Trust Device
        var device = await securityService.TrustDeviceAsync("user-99", "fingerprint-abc", "MacBook Pro", "macOS", "Safari", 90);
        Assert.True(device.IsTrusted);

        // 3. Assess Risk
        var riskResult = await riskEngine.CalculateRiskAsync("user-99", "192.168.1.10", "Safari", "fingerprint-abc");
        Assert.NotNull(riskResult);

        // 4. Secret Rotation
        var secretMeta = await secretsManager.RotateSecretAsync("JwtEncryptionKey", "integration-tester");
        Assert.True(secretMeta.Version >= 1);

        // 5. Compliance Report Generation
        var report = await complianceService.GenerateReportAsync("GDPR", "integration-tester");
        Assert.Equal("GDPR", report.FrameworkType);
    }
}
