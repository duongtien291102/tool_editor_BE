using AiVideoStudio.Application.Interfaces.OperationsAdmin;
using AiVideoStudio.Domain.Entities.OperationsAdmin;
using AiVideoStudio.Domain.Interfaces.OperationsAdmin;
using Microsoft.Extensions.Logging;

namespace AiVideoStudio.Application.Features.OperationsAdmin.Services;

public sealed class IncidentManager : IIncidentManager
{
    private readonly IPlatformAdministrationRepository _repository;
    private readonly ILogger<IncidentManager> _logger;

    public IncidentManager(
        IPlatformAdministrationRepository repository,
        ILogger<IncidentManager> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PlatformIncident> CreateIncidentAsync(string title, string description, string severity, string createdBy, CancellationToken cancellationToken = default)
    {
        var incident = PlatformIncident.Create(title, description, severity, createdBy);
        await _repository.SaveIncidentAsync(incident, cancellationToken);
        _logger.LogWarning("Platform incident created [{Severity}]: {Title}", severity, title);
        return incident;
    }

    public async Task<PlatformIncident?> AssignIncidentAsync(string incidentId, string assignee, string updatedBy, CancellationToken cancellationToken = default)
    {
        var incident = await _repository.GetIncidentByIdAsync(incidentId, cancellationToken);
        if (incident == null) return null;

        incident.Assign(assignee, updatedBy);
        await _repository.SaveIncidentAsync(incident, cancellationToken);
        return incident;
    }

    public async Task<PlatformIncident?> ResolveIncidentAsync(string incidentId, string rootCause, string resolution, string updatedBy, CancellationToken cancellationToken = default)
    {
        var incident = await _repository.GetIncidentByIdAsync(incidentId, cancellationToken);
        if (incident == null) return null;

        incident.Resolve(rootCause, resolution, updatedBy);
        await _repository.SaveIncidentAsync(incident, cancellationToken);
        _logger.LogInformation("Incident resolved: {Title}", incident.Title);
        return incident;
    }

    public async Task<IReadOnlyList<PlatformIncident>> GetIncidentsAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        return await _repository.GetIncidentsAsync(status, cancellationToken);
    }
}
