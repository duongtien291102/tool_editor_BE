using AiVideoStudio.Application.Features.OperationsAdmin.Services;
using AiVideoStudio.Domain.Entities.OperationsAdmin;
using AiVideoStudio.Domain.Interfaces.OperationsAdmin;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace AiVideoStudio.UnitTests.OperationsAdmin;

public class FeatureFlagTests
{
    [Fact]
    public async Task FeatureFlagService_GetAndSet_WorksCorrectly()
    {
        var repo = Substitute.For<IPlatformAdministrationRepository>();
        var defaultConfig = PlatformConfiguration.CreateDefault();
        repo.GetConfigurationAsync(Arg.Any<CancellationToken>()).Returns(defaultConfig);

        var service = new FeatureFlagService(repo, NullLogger<FeatureFlagService>.Instance);
        var enabled = await service.IsEnabledAsync("EnableDistributedScheduler");

        Assert.True(enabled);

        await service.SetFlagAsync("NewFlag", true, "admin-1");
        var newFlag = await service.IsEnabledAsync("NewFlag");
        Assert.True(newFlag);
    }
}

public class AuditTests
{
    [Fact]
    public async Task AuditService_LogAsync_CallsRepository()
    {
        var repo = Substitute.For<IPlatformAdministrationRepository>();
        var service = new AuditService(repo, NullLogger<AuditService>.Instance);

        await service.LogAsync("user-1", "John", "UPDATE", "Config", "123", new { A = 1 }, new { A = 2 });

        await repo.Received(1).AddAuditLogAsync(Arg.Any<PlatformAuditLogEntry>(), Arg.Any<CancellationToken>());
    }
}

public class BackupTests
{
    [Fact]
    public async Task BackupService_CreateBackupAsync_ReturnsValidatedSnapshot()
    {
        var repo = Substitute.For<IPlatformAdministrationRepository>();
        var service = new BackupService(repo, NullLogger<BackupService>.Instance);

        var snapshot = await service.CreateBackupAsync("Full", "admin-1");

        Assert.NotNull(snapshot);
        Assert.Equal("Validated", snapshot.Status);
        await repo.Received(1).SaveBackupSnapshotAsync(Arg.Any<BackupSnapshot>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BackupService_RestoreBackupAsync_MarksRestored()
    {
        var repo = Substitute.For<IPlatformAdministrationRepository>();
        var snapshot = BackupSnapshot.Create("test_backup", "Full", "./backups/test.tar.gz", 1024, "checksum", "admin-1");
        repo.GetBackupsAsync(Arg.Any<CancellationToken>()).Returns(new List<BackupSnapshot> { snapshot });

        var service = new BackupService(repo, NullLogger<BackupService>.Instance);
        var result = await service.RestoreBackupAsync(snapshot.Id, "admin-1");

        Assert.True(result);
        Assert.Equal("Restored", snapshot.Status);
    }
}

public class IncidentTests
{
    [Fact]
    public async Task IncidentManager_CreateAndResolve_UpdatesStatus()
    {
        var repo = Substitute.For<IPlatformAdministrationRepository>();
        var manager = new IncidentManager(repo, NullLogger<IncidentManager>.Instance);

        var incident = await manager.CreateIncidentAsync("High CPU", "CPU usage 99%", "P1", "system");
        Assert.Equal("Open", incident.Status);

        repo.GetIncidentByIdAsync(incident.Id, Arg.Any<CancellationToken>()).Returns(incident);

        var resolved = await manager.ResolveIncidentAsync(incident.Id, "Memory Leak", "Restarted service", "admin-1");
        Assert.NotNull(resolved);
        Assert.Equal("Resolved", resolved.Status);
        Assert.Equal("Memory Leak", resolved.RootCause);
    }
}

public class OperationsDashboardTests
{
    [Fact]
    public async Task PlatformHealthService_GetSnapshot_ReturnsAllSubsystems()
    {
        var healthService = new PlatformHealthService();
        var snapshot = await healthService.GetOperationsDashboardSnapshotAsync();

        Assert.NotNull(snapshot);
        Assert.Equal(12, snapshot.SubsystemHealth.Count);
        Assert.Equal("Healthy", snapshot.ClusterStatus);
    }
}
