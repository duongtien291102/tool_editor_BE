using System.Text.Json;
using AiVideoStudio.Application.Interfaces.OperationsAdmin;
using AiVideoStudio.Domain.Entities.OperationsAdmin;
using AiVideoStudio.Domain.Interfaces.OperationsAdmin;
using Microsoft.Extensions.Logging;

namespace AiVideoStudio.Application.Features.OperationsAdmin.Services;

public sealed class AuditService : IAuditService
{
    private readonly IPlatformAdministrationRepository _repository;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        IPlatformAdministrationRepository repository,
        ILogger<AuditService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task LogAsync(
        string userId,
        string userName,
        string action,
        string resource,
        string resourceId,
        object? beforeState = null,
        object? afterState = null,
        string? correlationId = null,
        string? traceId = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        string? beforeJson = beforeState != null ? JsonSerializer.Serialize(beforeState) : null;
        string? afterJson = afterState != null ? JsonSerializer.Serialize(afterState) : null;

        var entry = PlatformAuditLogEntry.Create(
            userId,
            userName,
            action,
            resource,
            resourceId,
            beforeJson,
            afterJson,
            correlationId ?? Guid.NewGuid().ToString("N"),
            traceId ?? string.Empty,
            ipAddress ?? "127.0.0.1",
            userAgent ?? "internal");

        await _repository.AddAuditLogAsync(entry, cancellationToken);
        _logger.LogInformation("Audit recorded: {Action} on {Resource}:{ResourceId} by {User}", action, resource, resourceId, userName);
    }

    public async Task<IReadOnlyList<PlatformAuditLogEntry>> QueryAuditLogsAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _repository.GetAuditLogsAsync(skip, take, cancellationToken);
    }
}
