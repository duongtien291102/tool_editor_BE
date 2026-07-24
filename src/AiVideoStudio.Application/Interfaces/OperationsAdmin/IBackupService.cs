using AiVideoStudio.Domain.Entities.OperationsAdmin;

namespace AiVideoStudio.Application.Interfaces.OperationsAdmin;

public interface IBackupService
{
    Task<BackupSnapshot> CreateBackupAsync(string backupType, string createdBy, CancellationToken cancellationToken = default);
    Task<bool> RestoreBackupAsync(string snapshotId, string requestedBy, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackupSnapshot>> GetBackupsAsync(CancellationToken cancellationToken = default);
}
