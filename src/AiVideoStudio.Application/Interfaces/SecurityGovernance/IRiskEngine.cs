namespace AiVideoStudio.Application.Interfaces.SecurityGovernance;

public record RiskAssessmentResult(double OverallRiskScore, string RiskLevel, bool RequiresMfa, IReadOnlyList<string> RiskFactors);

public interface IRiskEngine
{
    Task<RiskAssessmentResult> CalculateRiskAsync(
        string userId,
        string clientIp,
        string userAgent,
        string? deviceFingerprint,
        CancellationToken cancellationToken = default);
}
