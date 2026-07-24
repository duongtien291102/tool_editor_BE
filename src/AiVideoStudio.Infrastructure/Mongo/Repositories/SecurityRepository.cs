using AiVideoStudio.Domain.Entities.SecurityGovernance;
using AiVideoStudio.Domain.Interfaces.SecurityGovernance;
using MongoDB.Driver;

namespace AiVideoStudio.Infrastructure.Mongo.Repositories;

public sealed class SecurityRepository : ISecurityRepository
{
    private readonly MongoDbContext _context;

    public SecurityRepository(MongoDbContext context)
    {
        _context = context;
    }

    private IMongoCollection<SecurityPolicy> Policies => _context.Database.GetCollection<SecurityPolicy>("security_policies");
    private IMongoCollection<SecurityIncidentRecord> Incidents => _context.Database.GetCollection<SecurityIncidentRecord>("security_incidents");
    private IMongoCollection<TrustedDevice> TrustedDevices => _context.Database.GetCollection<TrustedDevice>("trusted_devices");
    private IMongoCollection<UserSecurityProfile> UserProfiles => _context.Database.GetCollection<UserSecurityProfile>("user_security_profiles");
    private IMongoCollection<ApiRateLimitPolicy> RateLimitPolicies => _context.Database.GetCollection<ApiRateLimitPolicy>("api_rate_limit_policies");
    private IMongoCollection<ComplianceReport> ComplianceReports => _context.Database.GetCollection<ComplianceReport>("compliance_reports");

    public async Task<SecurityPolicy?> GetActiveSecurityPolicyAsync(CancellationToken cancellationToken = default)
    {
        return await Policies.Find(FilterDefinition<SecurityPolicy>.Empty).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveSecurityPolicyAsync(SecurityPolicy policy, CancellationToken cancellationToken = default)
    {
        await Policies.ReplaceOneAsync(
            p => p.Id == policy.Id,
            policy,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task SaveSecurityIncidentAsync(SecurityIncidentRecord incident, CancellationToken cancellationToken = default)
    {
        await Incidents.ReplaceOneAsync(
            i => i.Id == incident.Id,
            incident,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<SecurityIncidentRecord?> GetSecurityIncidentByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Incidents.Find(i => i.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityIncidentRecord>> GetSecurityIncidentsAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var filter = string.IsNullOrWhiteSpace(status)
            ? FilterDefinition<SecurityIncidentRecord>.Empty
            : Builders<SecurityIncidentRecord>.Filter.Eq(i => i.Status, status);

        return await Incidents.Find(filter).SortByDescending(i => i.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TrustedDevice>> GetUserDevicesAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await TrustedDevices.Find(d => d.UserId == userId).ToListAsync(cancellationToken);
    }

    public async Task<TrustedDevice?> GetDeviceByFingerprintAsync(string userId, string fingerprint, CancellationToken cancellationToken = default)
    {
        return await TrustedDevices.Find(d => d.UserId == userId && d.DeviceFingerprint == fingerprint).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveTrustedDeviceAsync(TrustedDevice device, CancellationToken cancellationToken = default)
    {
        await TrustedDevices.ReplaceOneAsync(
            d => d.Id == device.Id,
            device,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task DeleteTrustedDeviceAsync(string id, CancellationToken cancellationToken = default)
    {
        await TrustedDevices.DeleteOneAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<UserSecurityProfile?> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await UserProfiles.Find(u => u.UserId == userId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveUserProfileAsync(UserSecurityProfile profile, CancellationToken cancellationToken = default)
    {
        await UserProfiles.ReplaceOneAsync(
            u => u.Id == profile.Id,
            profile,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<IReadOnlyList<ApiRateLimitPolicy>> GetRateLimitPoliciesAsync(CancellationToken cancellationToken = default)
    {
        return await RateLimitPolicies.Find(FilterDefinition<ApiRateLimitPolicy>.Empty).ToListAsync(cancellationToken);
    }

    public async Task SaveRateLimitPolicyAsync(ApiRateLimitPolicy policy, CancellationToken cancellationToken = default)
    {
        await RateLimitPolicies.ReplaceOneAsync(
            p => p.Id == policy.Id,
            policy,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task SaveComplianceReportAsync(ComplianceReport report, CancellationToken cancellationToken = default)
    {
        await ComplianceReports.ReplaceOneAsync(
            c => c.Id == report.Id,
            report,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<IReadOnlyList<ComplianceReport>> GetComplianceReportsAsync(string? frameworkType = null, CancellationToken cancellationToken = default)
    {
        var filter = string.IsNullOrWhiteSpace(frameworkType)
            ? FilterDefinition<ComplianceReport>.Empty
            : Builders<ComplianceReport>.Filter.Eq(c => c.FrameworkType, frameworkType.ToUpperInvariant());

        return await ComplianceReports.Find(filter).SortByDescending(c => c.GeneratedAt).ToListAsync(cancellationToken);
    }
}
