using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Storage;

public interface IStorageProvider
{
    Task<string> UploadAsync(string bucketName, string objectKey, Stream data, string mimeType, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);
}
