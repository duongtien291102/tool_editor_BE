using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Auth;

public class PermissionResolver : IPermissionResolver
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public PermissionResolver(IUserRepository userRepository, IRoleRepository roleRepository, IPermissionRepository permissionRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
    }

    public async Task<IEnumerable<string>> GetRolesForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null || !user.RoleIds.Any())
            return Enumerable.Empty<string>();

        var roles = await _roleRepository.GetRolesByIdsAsync(user.RoleIds, cancellationToken);
        return roles.Select(r => r.Name);
    }

    public async Task<IEnumerable<string>> GetPermissionsForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null || !user.RoleIds.Any())
            return Enumerable.Empty<string>();

        var roles = await _roleRepository.GetRolesByIdsAsync(user.RoleIds, cancellationToken);
        var permissionIds = roles.SelectMany(r => r.PermissionIds).Distinct();

        var permissions = await _permissionRepository.GetPermissionsByIdsAsync(permissionIds, cancellationToken);
        return permissions.Select(p => p.Code);
    }
}
