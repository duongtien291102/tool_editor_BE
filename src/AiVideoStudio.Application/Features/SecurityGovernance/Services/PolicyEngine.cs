using AiVideoStudio.Application.Interfaces.SecurityGovernance;
using AiVideoStudio.Domain.Interfaces.SecurityGovernance;
using Microsoft.Extensions.Logging;

namespace AiVideoStudio.Application.Features.SecurityGovernance.Services;

public sealed class PolicyEngine : IPolicyEngine
{
    private readonly ISecurityRepository _repository;
    private readonly ILogger<PolicyEngine> _logger;

    public PolicyEngine(
        ISecurityRepository repository,
        ILogger<PolicyEngine> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> EvaluateAsync(AuthorizationContext context, CancellationToken cancellationToken = default)
    {
        var policy = await _repository.GetActiveSecurityPolicyAsync(cancellationToken);
        if (policy == null) return true; // Default allow if no policy configured

        // 1. RBAC Evaluation
        bool rbacPassed = false;
        foreach (var role in context.Roles)
        {
            if (policy.RbacRules.TryGetValue(role, out var allowedPermissions))
            {
                string targetPermission = $"{context.Resource}:{context.Action}";
                if (allowedPermissions.Contains("*") || allowedPermissions.Contains("*:*") || allowedPermissions.Contains(targetPermission))
                {
                    rbacPassed = true;
                    break;
                }
            }
        }

        if (!rbacPassed && !context.Roles.Contains("Admin") && !context.Roles.Contains("SuperAdmin"))
        {
            _logger.LogWarning("RBAC denied for user {User} on {Resource}:{Action}", context.UserId, context.Resource, context.Action);
            return false;
        }

        // 2. ABAC Rules Evaluation
        foreach (var rule in policy.AbacRules)
        {
            if (rule.Attribute == "ClientIp" && rule.Effect == "Deny")
            {
                if (rule.ExpectedValue.Contains(context.ClientIp))
                {
                    _logger.LogWarning("ABAC IP Deny rule triggered for IP {Ip}", context.ClientIp);
                    return false;
                }
            }
        }

        return true;
    }
}
