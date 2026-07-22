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

        var baseDir = string.IsNullOrWhiteSpace(_options.BasePath) ? "./uploads" : _options.BasePath;
        var targetFolder = string.IsNullOrWhiteSpace(bucketName) ? baseDir : Path.Combine(baseDir, bucketName);

        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        var fullPath = Path.Combine(targetFolder, objectKey);

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
        var baseDir = string.IsNullOrWhiteSpace(_options.BasePath) ? "./uploads" : _options.BasePath;
        var targetFolder = string.IsNullOrWhiteSpace(bucketName) ? baseDir : Path.Combine(baseDir, bucketName);
        var fullPath = Path.Combine(targetFolder, objectKey);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found at storage path: {fullPath}");
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
    {
        var baseDir = string.IsNullOrWhiteSpace(_options.BasePath) ? "./uploads" : _options.BasePath;
        var targetFolder = string.IsNullOrWhiteSpace(bucketName) ? baseDir : Path.Combine(baseDir, bucketName);
        var fullPath = Path.Combine(targetFolder, objectKey);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
    {
        var baseDir = string.IsNullOrWhiteSpace(_options.BasePath) ? "./uploads" : _options.BasePath;
        var targetFolder = string.IsNullOrWhiteSpace(bucketName) ? baseDir : Path.Combine(baseDir, bucketName);
        var fullPath = Path.Combine(targetFolder, objectKey);

        return Task.FromResult(File.Exists(fullPath));
    }
}
