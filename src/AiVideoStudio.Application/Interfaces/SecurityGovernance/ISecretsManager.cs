namespace AiVideoStudio.Application.Interfaces.SecurityGovernance;

public record SecretKeyMetadata(string KeyName, int Version, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, bool IsActive);

public interface ISecretsManager
{
    Task<string> GetSecretAsync(string keyName, CancellationToken cancellationToken = default);
    Task<SecretKeyMetadata> RotateSecretAsync(string keyName, string rotatedBy, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecretKeyMetadata>> GetSecretMetadataAsync(CancellationToken cancellationToken = default);
}
