using AiVideoStudio.Domain.Entities;

namespace AiVideoStudio.Domain.Interfaces;

public interface IWorkspaceRepository
{
    Task<Workspace?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task AddAsync(Workspace entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Workspace entity, CancellationToken cancellationToken = default);
}
