using AiVideoStudio.Domain.Enums;
using System;

namespace AiVideoStudio.Application.Features.Media.DTOs;

public record MediaDto(
    string Id,
    string ProjectId,
    string OwnerId,
    string FileName,
    string OriginalFileName,
    string FileExtension,
    string MimeType,
    long FileSize,
    int? Width,
    int? Height,
    double? Duration,
    string StoragePath,
    string? ThumbnailPath,
    AssetType AssetType,
    MediaStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
