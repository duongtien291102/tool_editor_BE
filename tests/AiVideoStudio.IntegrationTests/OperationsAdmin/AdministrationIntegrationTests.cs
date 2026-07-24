using AiVideoStudio.Application.Features.OperationsAdmin.Services;
using AiVideoStudio.Domain.Entities.OperationsAdmin;
using AiVideoStudio.Domain.Interfaces.OperationsAdmin;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace AiVideoStudio.IntegrationTests.OperationsAdmin;

public class AdministrationIntegrationTests
{
    [Fact]
    public async Task PlatformAdministration_ConfigurationAndIncidents_IntegrationTest()
    {
        var repo = Substitute.For<IPlatformAdministrationRepository>();
        PlatformConfiguration? currentConfig = null;
        PlatformIncident? currentIncident = null;
        var auditList = new List<PlatformAuditLogEntry>();

        repo.GetConfigurationAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(currentConfig));
        repo.SaveConfigurationAsync(Arg.Do<PlatformConfiguration>(c => currentConfig = c), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        
        repo.SaveIncidentAsync(Arg.Do<PlatformIncident>(i => currentIncident = i), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        repo.GetIncidentByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(currentIncident));

        repo.AddAuditLogAsync(Arg.Do<PlatformAuditLogEntry>(a => auditList.Add(a)), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        repo.GetAuditLogsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult<IReadOnlyList<PlatformAuditLogEntry>>(auditList));

        var auditService = new AuditService(repo, NullLogger<AuditService>.Instance);
        var adminService = new PlatformAdministrationService(repo, auditService, NullLogger<PlatformAdministrationService>.Instance);
        var incidentManager = new IncidentManager(repo, NullLogger<IncidentManager>.Instance);

        // 1. Get & Update Config
        var config = await adminService.GetConfigurationAsync();
        Assert.NotNull(config);

        await adminService.UpdateConfigurationAsync(45, 15, 80, "*/10 * * * *", true, "integration-tester");
        var updatedConfig = await adminService.GetConfigurationAsync();
        Assert.Equal(45, updatedConfig.RetentionDays);

        // 2. Incident Lifecycle
        var incident = await incidentManager.CreateIncidentAsync("Database Delay", "High latency on Mongo query", "P2", "tester");
        Assert.NotNull(incident.Id);

        var resolved = await incidentManager.ResolveIncidentAsync(incident.Id, "Missing Index", "Added index to collection", "tester");
        Assert.NotNull(resolved);
        Assert.Equal("Resolved", resolved.Status);

        // 3. Audit Log Query
        var logs = await auditService.QueryAuditLogsAsync();
        Assert.NotEmpty(logs);
    }
}
