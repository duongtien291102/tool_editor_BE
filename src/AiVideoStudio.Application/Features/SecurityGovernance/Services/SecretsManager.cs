using System.Collections.Concurrent;
using System.Security.Cryptography;
using AiVideoStudio.Application.Interfaces.SecurityGovernance;
using Microsoft.Extensions.Logging;

namespace AiVideoStudio.Application.Features.SecurityGovernance.Services;

public sealed class SecretsManager : ISecretsManager
{
    private readonly ConcurrentDictionary<string, (string Value, SecretKeyMetadata Metadata)> _secrets = new();
    private readonly ILogger<SecretsManager> _logger;

    public SecretsManager(ILogger<SecretsManager> logger)
    {
        _logger = logger;
        InitializeDefaultSecrets();
    }

    public Task<string> GetSecretAsync(string keyName, CancellationToken cancellationToken = default)
    {
        if (_secrets.TryGetValue(keyName, out var entry))
        {
            return Task.FromResult(entry.Value);
        }
        return Task.FromResult(string.Empty);
    }

    public Task<SecretKeyMetadata> RotateSecretAsync(string keyName, string rotatedBy, CancellationToken cancellationToken = default)
    {
        int newVersion = 1;
        if (_secrets.TryGetValue(keyName, out var current))
        {
            newVersion = current.Metadata.Version + 1;
        }

        byte[] randomBytes = new byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        string newValue = Convert.ToBase64String(randomBytes);

        var metadata = new SecretKeyMetadata(keyName, newVersion, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(90), true);
        _secrets[keyName] = (newValue, metadata);

        _logger.LogInformation("Rotated secret key '{KeyName}' to version {Version} by {RotatedBy}", keyName, newVersion, rotatedBy);
        return Task.FromResult(metadata);
    }

    public Task<IReadOnlyList<SecretKeyMetadata>> GetSecretMetadataAsync(CancellationToken cancellationToken = default)
    {
        var list = _secrets.Values.Select(v => v.Metadata).ToList();
        return Task.FromResult<IReadOnlyList<SecretKeyMetadata>>(list);
    }

    private void InitializeDefaultSecrets()
    {
        _secrets["JwtEncryptionKey"] = ("super_secret_jwt_encryption_key_32bytes!!", new SecretKeyMetadata("JwtEncryptionKey", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(90), true));
        _secrets["CookieStorageMasterKey"] = ("master_cookie_storage_aes_256_key_32b", new SecretKeyMetadata("CookieStorageMasterKey", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(90), true));
    }
}
