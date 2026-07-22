using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Events;
using AiVideoStudio.Application.Features.Media.Commands;
using AiVideoStudio.Application.Features.Media.DTOs;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Application.Storage;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Features.Media.Handlers;

public class UploadMediaCommandHandler : IRequestHandler<UploadMediaCommand, Result<MediaDto>>
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IStorageProvider _storageProvider;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus;
    private readonly StorageOptions _storageOptions;

    public UploadMediaCommandHandler(
        IMediaAssetRepository mediaAssetRepository,
        IProjectRepository projectRepository,
        IStorageProvider storageProvider,
        ICurrentUser currentUser,
        IMapper mapper,
        IEventBus eventBus,
        IOptions<StorageOptions> storageOptions)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _projectRepository = projectRepository;
        _storageProvider = storageProvider;
        _currentUser = currentUser;
        _mapper = mapper;
        _eventBus = eventBus;
        _storageOptions = storageOptions.Value;
    }

    public async Task<Result<MediaDto>> Handle(UploadMediaCommand command, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result<MediaDto>.Failure(AuthErrors.Unauthorized);
        }

        if (string.IsNullOrWhiteSpace(command.FileName) || command.FileSize <= 0 || command.ContentStream == null)
        {
            return Result<MediaDto>.Failure(MediaErrors.InvalidPayload);
        }

        var project = await _projectRepository.GetByIdAsync(command.ProjectId, cancellationToken);
        if (project == null || project.IsDeleted)
        {
            return Result<MediaDto>.Failure(MediaErrors.ProjectNotFound);
        }

        var isOwner = project.OwnerId == _currentUser.UserId;
        var isAdmin = _currentUser.Roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase) || r.Equals("Administrator", StringComparison.OrdinalIgnoreCase));

        if (!isOwner && !isAdmin)
        {
            return Result<MediaDto>.Failure(MediaErrors.UnauthorizedAccess);
        }

        if (_storageOptions.MaxFileSizeBytes > 0 && command.FileSize > _storageOptions.MaxFileSizeBytes)
        {
            return Result<MediaDto>.Failure(MediaErrors.FileTooLarge);
        }

        var extension = Path.GetExtension(command.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            return Result<MediaDto>.Failure(MediaErrors.InvalidFileType);
        }

        if (_storageOptions.AllowedExtensions != null && _storageOptions.AllowedExtensions.Count > 0)
        {
            if (!_storageOptions.AllowedExtensions.Any(e => e.Equals(extension, StringComparison.OrdinalIgnoreCase)))
            {
                return Result<MediaDto>.Failure(MediaErrors.InvalidFileType);
            }
        }

        if (_storageOptions.AllowedMimeTypes != null && _storageOptions.AllowedMimeTypes.Count > 0 && !string.IsNullOrWhiteSpace(command.MimeType))
        {
            if (!_storageOptions.AllowedMimeTypes.Any(m => m.Equals(command.MimeType, StringComparison.OrdinalIgnoreCase)))
            {
                return Result<MediaDto>.Failure(MediaErrors.InvalidFileType);
            }
        }

        var assetType = ResolveAssetType(extension, command.MimeType);
        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";

        string storagePath;
        try
        {
            storagePath = await _storageProvider.UploadAsync(command.ProjectId, uniqueFileName, command.ContentStream, command.MimeType, cancellationToken);
        }
        catch (Exception)
        {
            return Result<MediaDto>.Failure(MediaErrors.UploadFailed);
        }

        var mediaAsset = MediaAsset.Create(
            command.ProjectId,
            _currentUser.UserId,
            uniqueFileName,
            command.FileName,
            extension,
            command.MimeType,
            command.FileSize,
            storagePath,
            assetType
        );

        await _mediaAssetRepository.AddAsync(mediaAsset, cancellationToken);

        foreach (var domainEvent in mediaAsset.DomainEvents)
        {
            await _eventBus.PublishAsync(domainEvent, cancellationToken);
        }
        mediaAsset.ClearDomainEvents();

        var dto = _mapper.Map<MediaDto>(mediaAsset);
        return Result<MediaDto>.Success(dto);
    }

    private static AssetType ResolveAssetType(string extension, string mimeType)
    {
        var ext = extension.TrimStart('.').ToLowerInvariant();
        var mime = (mimeType ?? string.Empty).ToLowerInvariant();

        if (mime.StartsWith("image/") || ext is "jpg" or "jpeg" or "png" or "gif" or "webp" or "svg")
            return AssetType.Image;

        if (mime.StartsWith("video/") || ext is "mp4" or "mov" or "avi" or "mkv" or "webm")
            return AssetType.Video;

        if (mime.StartsWith("audio/") || ext is "mp3" or "wav" or "aac" or "m4a" or "ogg" or "flac")
            return AssetType.Audio;

        if (mime.Contains("subrip") || mime.Contains("vtt") || ext is "vtt" or "srt")
            return AssetType.Subtitle;

        if (mime.Contains("font") || ext is "ttf" or "otf" or "woff" or "woff2")
            return AssetType.Font;

        return AssetType.Other;
    }
}
