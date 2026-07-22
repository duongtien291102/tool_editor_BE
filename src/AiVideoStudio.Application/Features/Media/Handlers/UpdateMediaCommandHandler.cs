using AiVideoStudio.Application.Features.Media.Commands;
using AiVideoStudio.Application.Features.Media.DTOs;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using AutoMapper;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Features.Media.Handlers;

public class UpdateMediaCommandHandler : IRequestHandler<UpdateMediaCommand, Result<MediaDto>>
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public UpdateMediaCommandHandler(
        IMediaAssetRepository mediaAssetRepository,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<MediaDto>> Handle(UpdateMediaCommand command, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result<MediaDto>.Failure(AuthErrors.Unauthorized);
        }

        var mediaAsset = await _mediaAssetRepository.GetByIdAsync(command.Id, cancellationToken);
        if (mediaAsset == null || mediaAsset.IsDeleted)
        {
            return Result<MediaDto>.Failure(MediaErrors.NotFound);
        }

        var isOwner = mediaAsset.OwnerId == _currentUser.UserId;
        var isAdmin = _currentUser.Roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase) || r.Equals("Administrator", StringComparison.OrdinalIgnoreCase));

        if (!isOwner && !isAdmin)
        {
            return Result<MediaDto>.Failure(MediaErrors.UnauthorizedAccess);
        }

        mediaAsset.UpdateMetadata(
            command.FileName,
            command.Width,
            command.Height,
            command.Duration,
            command.ThumbnailPath,
            _currentUser.UserId
        );

        await _mediaAssetRepository.UpdateAsync(mediaAsset, cancellationToken);

        var dto = _mapper.Map<MediaDto>(mediaAsset);
        return Result<MediaDto>.Success(dto);
    }
}
