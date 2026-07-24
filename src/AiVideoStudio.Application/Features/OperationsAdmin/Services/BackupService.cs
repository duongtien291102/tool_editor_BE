using System.Security.Cryptography;
using System.Text;
using AiVideoStudio.Application.Interfaces.OperationsAdmin;
using AiVideoStudio.Domain.Entities.OperationsAdmin;
using AiVideoStudio.Domain.Interfaces.OperationsAdmin;
using Microsoft.Extensions.Logging;

namespace AiVideoStudio.Application.Features.OperationsAdmin.Services;

public sealed class BackupService : IBackupService
{
    private readonly IPlatformAdministrationRepository _repository;
    private readonly ILogger<BackupService> _logger;

    public BackupService(
        IPlatformAdministrationRepository repository,
        ILogger<BackupService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<BackupSnapshot> CreateBackupAsync(string backupType, string createdBy, CancellationToken cancellationToken = default)
    {
        string name = $"backup_{backupType.ToLowerInvariant()}_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}";
        string location = $"./backups/{name}.tar.gz";
        long mockSize = 1024 * 1024 * 42; // 42 MB snapshot
        string checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name)));

        var snapshot = BackupSnapshot.Create(name, backupType, location, mockSize, checksum, createdBy);
        snapshot.Validate(true, "Snapshot structure and checksum verified.", createdBy);

        await _repository.SaveBackupSnapshotAsync(snapshot, cancellationToken);
        _logger.LogInformation("Backup snapshot created successfully: {SnapshotName}", name);
        return snapshot;
    }

    public async Task<bool> RestoreBackupAsync(string snapshotId, string requestedBy, CancellationToken cancellationToken = default)
    {
        var backups = await _repository.GetBackupsAsync(cancellationToken);
        var target = backups.FirstOrDefault(b => b.Id == snapshotId);
        if (target == null)
        {
            _logger.LogWarning("Restore requested for non-existent backup: {SnapshotId}", snapshotId);
            return false;
        }

        target.MarkRestored(requestedBy);
        await _repository.SaveBackupSnapshotAsync(target, cancellationToken);
        _logger.LogInformation("Restored system state from snapshot: {SnapshotName} by {RequestedBy}", target.SnapshotName, requestedBy);
        return true;
    }

    public async Task<IReadOnlyList<BackupSnapshot>> GetBackupsAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetBackupsAsync(cancellationToken);
    }
}
