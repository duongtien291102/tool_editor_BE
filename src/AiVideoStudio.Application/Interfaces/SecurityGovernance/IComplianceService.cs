using AiVideoStudio.Domain.Entities.SecurityGovernance;

namespace AiVideoStudio.Application.Interfaces.SecurityGovernance;

public interface IComplianceService
{
    Task<ComplianceReport> GenerateReportAsync(string frameworkType, string generatedBy, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComplianceReport>> GetReportsAsync(string? frameworkType = null, CancellationToken cancellationToken = default);
}
