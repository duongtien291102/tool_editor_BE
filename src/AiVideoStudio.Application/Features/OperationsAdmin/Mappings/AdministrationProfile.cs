using AutoMapper;
using AiVideoStudio.Domain.Entities.OperationsAdmin;

namespace AiVideoStudio.Application.Features.OperationsAdmin.Mappings;

public sealed record PlatformConfigurationDto(
    string Id,
    string Environment,
    IReadOnlyDictionary<string, bool> FeatureFlags,
    int RetentionDays,
    bool EnforceStrictSecurity,
    int MaxConcurrentJobsPerTenant,
    int MaxDailyExportsPerUser);

public sealed record PlatformIncidentDto(
    string Id,
    string Title,
    string Description,
    string Severity,
    string Status,
    string? AssignedTo,
    string? RootCause,
    string? Resolution,
    DateTimeOffset CreatedAt);

public sealed class AdministrationProfile : Profile
{
    public AdministrationProfile()
    {
        CreateMap<PlatformConfiguration, PlatformConfigurationDto>();
        CreateMap<PlatformIncident, PlatformIncidentDto>();
    }
}
