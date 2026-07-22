using AiVideoStudio.Domain.Base;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Events.Media;
using System;

namespace AiVideoStudio.Domain.Entities;

public class MediaAsset : BaseEntity
{
    public string ProjectId { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? Duration { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public AssetType AssetType { get; set; } = AssetType.Other;
    public MediaStatus Status { get; set; } = MediaStatus.Uploading;
    public bool IsDeleted => DeletedAt.HasValue;

    public MediaAsset()
    {
    }

    public static MediaAsset Create(
        string projectId,
        string ownerId,
        string fileName,
        string originalFileName,
        string fileExtension,
        string mimeType,
        long fileSize,
        string storagePath,
        AssetType assetType,
        int? width = null,
        int? height = null,
        double? duration = null,
        string? thumbnailPath = null)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new ArgumentException("ProjectId cannot be empty.", nameof(projectId));
        }

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException("OwnerId cannot be empty.", nameof(ownerId));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("FileName cannot be empty.", nameof(fileName));
        }

        if (fileSize <= 0)
        {
            throw new ArgumentException("FileSize must be greater than zero.", nameof(fileSize));
        }

        var media = new MediaAsset
        {
            ProjectId = projectId,
            OwnerId = ownerId,
            FileName = fileName.Trim(),
            OriginalFileName = string.IsNullOrWhiteSpace(originalFileName) ? fileName.Trim() : originalFileName.Trim(),
            FileExtension = fileExtension.Trim().ToLowerInvariant(),
            MimeType = mimeType.Trim().ToLowerInvariant(),
            FileSize = fileSize,
            StoragePath = storagePath.Trim(),
            ThumbnailPath = thumbnailPath?.Trim(),
            AssetType = assetType,
            Width = width,
            Height = height,
            Duration = duration,
            Status = MediaStatus.Ready,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = ownerId
        };

        media.AddDomainEvent(new MediaUploadedEvent(media.Id, projectId, ownerId));
        return media;
    }

    public void UpdateMetadata(string? fileName, int? width, int? height, double? duration, string? thumbnailPath, string updatedBy)
    {
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            FileName = fileName.Trim();
        }

        if (width.HasValue)
        {
            Width = width.Value;
        }

        if (height.HasValue)
        {
            Height = height.Value;
        }

        if (duration.HasValue)
        {
            Duration = duration.Value;
        }

        if (!string.IsNullOrWhiteSpace(thumbnailPath))
        {
            ThumbnailPath = thumbnailPath.Trim();
        }

        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void MarkProcessing()
    {
        Status = MediaStatus.Processing;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new MediaProcessingStartedEvent(Id));
    }

    public void MarkReady(string? thumbnailPath = null)
    {
        Status = MediaStatus.Ready;
        if (!string.IsNullOrWhiteSpace(thumbnailPath))
        {
            ThumbnailPath = thumbnailPath.Trim();
        }

        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new MediaProcessingCompletedEvent(Id, true));
    }

    public void MarkFailed()
    {
        Status = MediaStatus.Failed;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new MediaProcessingCompletedEvent(Id, false));
    }

    public void SoftDelete(string deletedBy)
    {
        if (IsDeleted)
            return;

        Status = MediaStatus.Deleted;
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;

        AddDomainEvent(new MediaDeletedEvent(Id, deletedBy));
    }
}
