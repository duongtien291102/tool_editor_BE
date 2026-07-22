using AiVideoStudio.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using AiVideoStudio.Application.Interfaces.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace AiVideoStudio.Infrastructure;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private IEnumerable<string>? _permissions;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue("userId") 
                             ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Username => _httpContextAccessor.HttpContext?.User?.FindFirstValue("username")
                               ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

    public IEnumerable<string> Roles => _httpContextAccessor.HttpContext?.User?.FindAll("role").Select(c => c.Value) ?? Enumerable.Empty<string>();

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string? CorrelationId => _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();
    
    public string? RequestId => _httpContextAccessor.HttpContext?.TraceIdentifier;

    public IEnumerable<string> Permissions
    {
        get
        {
            if (!IsAuthenticated || string.IsNullOrEmpty(UserId)) return Enumerable.Empty<string>();

            if (_permissions != null) return _permissions;

            // Lazy resolution of permissions
            var permissionResolver = _httpContextAccessor.HttpContext?.RequestServices.GetService<IPermissionResolver>();
            if (permissionResolver != null)
            {
                _permissions = permissionResolver.GetPermissionsForUserAsync(UserId).GetAwaiter().GetResult();
            }
            else
            {
                _permissions = Enumerable.Empty<string>();
            }

            return _permissions;
        }
    }
}
