using AiVideoStudio.Application.Interfaces.Auth;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Auth;

public class AuthorizationService : IAuthorizationService
{
    private readonly IPermissionResolver _permissionResolver;

    public AuthorizationService(IPermissionResolver permissionResolver)
    {
        _permissionResolver = permissionResolver;
    }

    public async Task<bool> HasPermissionAsync(string userId, string permissionCode, CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionResolver.GetPermissionsForUserAsync(userId, cancellationToken);
        return permissions.Contains(permissionCode);
    }

    public async Task<bool> HasRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        var roles = await _permissionResolver.GetRolesForUserAsync(userId, cancellationToken);
        return roles.Contains(roleName);
    }
}
