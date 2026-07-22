using AiVideoStudio.Application.Storage;

namespace AiVideoStudio.Infrastructure.Storage;

public class MinioStorageProvider : IStorageProvider
{
    public Task DeleteAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Stream> DownloadAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> UploadAsync(string bucketName, string objectKey, Stream data, string mimeType, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

