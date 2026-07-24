using AiVideoStudio.Domain.Entities.SecurityGovernance;

namespace AiVideoStudio.Application.Interfaces.SecurityGovernance;

public interface IThreatDetectionService
{
    Task AnalyzeActivityAsync(string userId, string ipAddress, string userAgent, string action, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityIncidentRecord>> GetIncidentsAsync(string? status = null, CancellationToken cancellationToken = default);
}
