using AiVideoStudio.Domain.Enums;
namespace AiVideoStudio.Application.Features.Uploads.DTOs;
public record ChunkDto(int Index, long Size, string Checksum, bool Completed);
public record ManifestDto(string AssetId, string FileName, string ContentType, long FileSize, string Checksum,
    string StoragePath, string? ThumbnailPath, AssetMetadataDto Metadata, DateTimeOffset CreatedAt);
public record AssetMetadataDto(int? Width, int? Height, double? Duration, string Kind, IReadOnlyDictionary<string,string> Properties);
public record UploadSessionDto(string Id, string AssetId, string ProjectId, string OwnerId, UploadStatus Status,
    string FileName, string ContentType, long FileSize, long UploadedBytes, int ChunkCount,
    IReadOnlyCollection<int> CompletedChunks, string Checksum, string? StoragePath, string? ManifestPath,
    string? ErrorMessage, int RetryCount, int Version, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, DateTimeOffset? CompletedAt);
public record UploadSummaryDto(string Id, string AssetId, string ProjectId, UploadStatus Status, string FileName,
    long FileSize, long UploadedBytes, int ChunkCount, int CompletedChunkCount, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);
