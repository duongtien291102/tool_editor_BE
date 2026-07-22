using AiVideoStudio.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Domain.Interfaces;

public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Permission>> GetPermissionsByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    Task AddAsync(Permission permission, CancellationToken cancellationToken = default);
}
