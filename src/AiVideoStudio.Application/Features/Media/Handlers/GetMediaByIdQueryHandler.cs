using AiVideoStudio.Application.Features.Media.DTOs;
using AiVideoStudio.Application.Features.Media.Queries;
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

public class GetMediaByIdQueryHandler : IRequestHandler<GetMediaByIdQuery, Result<MediaDto>>
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public GetMediaByIdQueryHandler(
        IMediaAssetRepository mediaAssetRepository,
        IProjectRepository projectRepository,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _projectRepository = projectRepository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<MediaDto>> Handle(GetMediaByIdQuery query, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result<MediaDto>.Failure(AuthErrors.Unauthorized);
        }

        var mediaAsset = await _mediaAssetRepository.GetByIdAsync(query.Id, cancellationToken);
        if (mediaAsset == null || mediaAsset.IsDeleted)
        {
            return Result<MediaDto>.Failure(MediaErrors.NotFound);
        }

        var project = await _projectRepository.GetByIdAsync(mediaAsset.ProjectId, cancellationToken);
        var isOwner = mediaAsset.OwnerId == _currentUser.UserId || (project != null && project.OwnerId == _currentUser.UserId);
        var isAdmin = _currentUser.Roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase) || r.Equals("Administrator", StringComparison.OrdinalIgnoreCase));

        if (!isOwner && !isAdmin)
        {
            return Result<MediaDto>.Failure(MediaErrors.UnauthorizedAccess);
        }

        var dto = _mapper.Map<MediaDto>(mediaAsset);
        return Result<MediaDto>.Success(dto);
    }
}
