using AiVideoStudio.Application.Features.Uploads.DTOs;
using AiVideoStudio.Domain.Entities;
namespace AiVideoStudio.Application.Storage;

public interface IChunkUploadEngine
{
    Task<ChunkDto> StoreChunkAsync(UploadSession session, int index, byte[] data, string checksum, CancellationToken cancellationToken = default);
    Task<string> MergeAsync(UploadSession session, CancellationToken cancellationToken = default);
    Task DeleteChunksAsync(string uploadId, CancellationToken cancellationToken = default);
}
public interface IThumbnailGenerator
{
    Task<string> GenerateImageThumbnailAsync(string projectId, string assetId, string sourcePath, CancellationToken cancellationToken = default);
    Task<string> GenerateVideoThumbnailAsync(string projectId, string assetId, string sourcePath, CancellationToken cancellationToken = default);
    Task<string> GenerateAudioWaveformAsync(string projectId, string assetId, string sourcePath, CancellationToken cancellationToken = default);
}
public interface IMetadataExtractor
{
    Task<AssetMetadataDto> ExtractImageAsync(Stream stream, CancellationToken cancellationToken = default);
    Task<AssetMetadataDto> ExtractVideoAsync(Stream stream, CancellationToken cancellationToken = default);
    Task<AssetMetadataDto> ExtractAudioAsync(Stream stream, CancellationToken cancellationToken = default);
    Task<AssetMetadataDto> ExtractSubtitleAsync(Stream stream, CancellationToken cancellationToken = default);
}
public interface IAssetManifestBuilder
{
    ManifestDto Build(UploadSession session, string storagePath, string? thumbnailPath, AssetMetadataDto metadata);
    Task<string> SaveAsync(string projectId, ManifestDto manifest, CancellationToken cancellationToken = default);
}
