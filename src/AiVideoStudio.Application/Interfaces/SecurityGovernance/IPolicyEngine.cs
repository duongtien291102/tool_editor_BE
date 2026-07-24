namespace AiVideoStudio.Application.Interfaces.SecurityGovernance;

public record AuthorizationContext(
    string UserId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Claims,
    string ClientIp,
    string UserAgent,
    string? TenantId,
    string Resource,
    string Action,
    IDictionary<string, object>? Attributes = null);

public interface IPolicyEngine
{
    Task<bool> EvaluateAsync(AuthorizationContext context, CancellationToken cancellationToken = default);
}
