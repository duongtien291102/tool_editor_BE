using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Entities.SecurityGovernance;

public sealed class ComplianceReport : BaseEntity
{
    public string FrameworkType { get; private set; } = "SOC2"; // GDPR, SOC2, ISO27001
    public string Status { get; private set; } = "Compliant"; // Compliant, NonCompliant, Warning
    public double ComplianceScore { get; private set; } = 95.5; // Percentage
    public DateTimeOffset GeneratedAt { get; private set; } = DateTimeOffset.UtcNow;
    public List<ComplianceFinding> Findings { get; private set; } = new();

    private ComplianceReport() { }

    public static ComplianceReport Create(string frameworkType, double score, string status, string generatedBy)
    {
        return new ComplianceReport
        {
            FrameworkType = frameworkType,
            ComplianceScore = score,
            Status = status,
            GeneratedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = generatedBy,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = generatedBy
        };
    }

    public void AddFinding(string category, string requirement, bool isPassed, string details)
    {
        Findings.Add(new ComplianceFinding(category, requirement, isPassed, details));
    }
}

public sealed record ComplianceFinding(string Category, string Requirement, bool IsPassed, string Details);
