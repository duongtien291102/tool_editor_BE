using AiVideoStudio.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Domain.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Role>> GetRolesByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    Task AddAsync(Role role, CancellationToken cancellationToken = default);
}
