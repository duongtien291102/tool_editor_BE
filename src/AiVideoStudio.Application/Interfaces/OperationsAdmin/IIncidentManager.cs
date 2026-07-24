using AiVideoStudio.Domain.Entities.OperationsAdmin;

namespace AiVideoStudio.Application.Interfaces.OperationsAdmin;

public interface IIncidentManager
{
    Task<PlatformIncident> CreateIncidentAsync(string title, string description, string severity, string createdBy, CancellationToken cancellationToken = default);
    Task<PlatformIncident?> AssignIncidentAsync(string incidentId, string assignee, string updatedBy, CancellationToken cancellationToken = default);
    Task<PlatformIncident?> ResolveIncidentAsync(string incidentId, string rootCause, string resolution, string updatedBy, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlatformIncident>> GetIncidentsAsync(string? status = null, CancellationToken cancellationToken = default);
}
