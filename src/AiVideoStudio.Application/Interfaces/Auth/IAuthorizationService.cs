using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Interfaces.Auth;

public interface IAuthorizationService
{
    Task<bool> HasPermissionAsync(string userId, string permissionCode, CancellationToken cancellationToken = default);
    Task<bool> HasRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default);
}
