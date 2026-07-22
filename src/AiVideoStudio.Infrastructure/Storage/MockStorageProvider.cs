using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Storage;
using Microsoft.Extensions.Options;

namespace AiVideoStudio.Infrastructure.Storage;

public class MockStorageProvider : IStorageProvider
{
    private readonly LocalStorageProvider _local;
    public MockStorageProvider(IOptions<StorageOptions> options) => _local = new LocalStorageProvider(options);
    public Task<string> UploadAsync(string bucketName, string objectKey, Stream data, string mimeType, CancellationToken ct = default) => _local.UploadAsync(bucketName, objectKey, data, mimeType, ct);
    public Task<Stream> DownloadAsync(string bucketName, string objectKey, CancellationToken ct = default) => _local.DownloadAsync(bucketName, objectKey, ct);
    public Task DeleteAsync(string bucketName, string objectKey, CancellationToken ct = default) => _local.DeleteAsync(bucketName, objectKey, ct);
    public Task<bool> ExistsAsync(string bucketName, string objectKey, CancellationToken ct = default) => _local.ExistsAsync(bucketName, objectKey, ct);
    public Task MoveAsync(string sb, string sk, string db, string dk, CancellationToken ct = default) => _local.MoveAsync(sb, sk, db, dk, ct);
    public Task CopyAsync(string sb, string sk, string db, string dk, CancellationToken ct = default) => _local.CopyAsync(sb, sk, db, dk, ct);
    public Task<Stream> OpenReadStreamAsync(string bucketName, string objectKey, CancellationToken ct = default) => _local.OpenReadStreamAsync(bucketName, objectKey, ct);
    public Task<Uri> GenerateTemporaryUrlAsync(string bucketName, string objectKey, TimeSpan lifetime, CancellationToken ct = default) => _local.GenerateTemporaryUrlAsync(bucketName, objectKey, lifetime, ct);
}
