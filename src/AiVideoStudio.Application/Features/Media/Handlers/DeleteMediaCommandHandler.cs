using AiVideoStudio.Application.Events;
using AiVideoStudio.Application.Features.Media.Commands;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Application.Storage;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Features.Media.Handlers;

public class DeleteMediaCommandHandler : IRequestHandler<DeleteMediaCommand, Result<bool>>
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IStorageProvider _storageProvider;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _eventBus;

    public DeleteMediaCommandHandler(
        IMediaAssetRepository mediaAssetRepository,
        IStorageProvider storageProvider,
        ICurrentUser currentUser,
        IEventBus eventBus)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _storageProvider = storageProvider;
        _currentUser = currentUser;
        _eventBus = eventBus;
    }

    public async Task<Result<bool>> Handle(DeleteMediaCommand command, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result<bool>.Failure(AuthErrors.Unauthorized);
        }

        var mediaAsset = await _mediaAssetRepository.GetByIdAsync(command.Id, cancellationToken);
        if (mediaAsset == null || mediaAsset.IsDeleted)
        {
            return Result<bool>.Failure(MediaErrors.NotFound);
        }

        var isOwner = mediaAsset.OwnerId == _currentUser.UserId;
        var isAdmin = _currentUser.Roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase) || r.Equals("Administrator", StringComparison.OrdinalIgnoreCase));

        if (!isOwner && !isAdmin)
        {
            return Result<bool>.Failure(MediaErrors.UnauthorizedAccess);
        }

        mediaAsset.SoftDelete(_currentUser.UserId);
        await _mediaAssetRepository.UpdateAsync(mediaAsset, cancellationToken);

        try
        {
            await _storageProvider.DeleteAsync(mediaAsset.ProjectId, mediaAsset.FileName, cancellationToken);
        }
        catch (Exception)
        {
            // Logging can happen here, file cleanup non-blocking for soft delete
        }

        foreach (var domainEvent in mediaAsset.DomainEvents)
        {
            await _eventBus.PublishAsync(domainEvent, cancellationToken);
        }
        mediaAsset.ClearDomainEvents();

        return Result<bool>.Success(true);
    }
}
