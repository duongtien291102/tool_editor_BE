using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Storage;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Storage;

public class LocalStorageProvider : IStorageProvider
{
    private readonly StorageOptions _options;

    public LocalStorageProvider(IOptions<StorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> UploadAsync(string bucketName, string objectKey, Stream data, string mimeType, CancellationToken cancellationToken = default)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        var fullPath = ResolvePath(bucketName, objectKey);

        var subDir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(subDir) && !Directory.Exists(subDir))
        {
            Directory.CreateDirectory(subDir);
        }

        using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true))
        {
            if (data.CanSeek)
            {
                data.Position = 0;
            }
            await data.CopyToAsync(fileStream, 8192, cancellationToken);
        }

        var relativePath = string.IsNullOrWhiteSpace(bucketName) 
            ? objectKey.Replace('\\', '/') 
            : Path.Combine(bucketName, objectKey).Replace('\\', '/');

        return relativePath;
    }

    public Task<Stream> DownloadAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(bucketName, objectKey);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found at storage path: {fullPath}");
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(bucketName, objectKey);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(bucketName, objectKey);

        return Task.FromResult(File.Exists(fullPath));
    }

    public async Task MoveAsync(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey, CancellationToken cancellationToken = default)
    {
        await CopyAsync(sourceBucket, sourceKey, destinationBucket, destinationKey, cancellationToken);
        await DeleteAsync(sourceBucket, sourceKey, cancellationToken);
    }

    public async Task CopyAsync(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey, CancellationToken cancellationToken = default)
    {
        await using var source = await OpenReadStreamAsync(sourceBucket, sourceKey, cancellationToken);
        await UploadAsync(destinationBucket, destinationKey, source, "application/octet-stream", cancellationToken);
    }

    public Task<Stream> OpenReadStreamAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
        => DownloadAsync(bucketName, objectKey, cancellationToken);

    public Task<Uri> GenerateTemporaryUrlAsync(string bucketName, string objectKey, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = ResolvePath(bucketName, objectKey);
        var path = $"{bucketName}/{objectKey}".Trim('/').Replace('\\', '/');
        return Task.FromResult(new Uri($"mock-storage://asset/{Uri.EscapeDataString(path)}?ttl={Math.Max(1, (int)lifetime.TotalSeconds)}"));
    }

    private string ResolvePath(string bucketName, string objectKey)
    {
        if (string.IsNullOrWhiteSpace(_options.BasePath))
            throw new InvalidOperationException("Storage BasePath is not configured.");
        if (string.IsNullOrWhiteSpace(objectKey))
            throw new ArgumentException("Object key is required.", nameof(objectKey));

        var root = Path.GetFullPath(_options.BasePath);
        var combined = string.IsNullOrWhiteSpace(bucketName)
            ? Path.Combine(root, objectKey)
            : Path.Combine(root, bucketName, objectKey);
        var resolved = Path.GetFullPath(combined);
        var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!resolved.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Storage path escapes the configured root.");
        return resolved;
    }
}
