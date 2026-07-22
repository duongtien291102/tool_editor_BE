using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Interfaces.Auth;

public interface IPermissionResolver
{
    Task<IEnumerable<string>> GetPermissionsForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetRolesForUserAsync(string userId, CancellationToken cancellationToken = default);
}
