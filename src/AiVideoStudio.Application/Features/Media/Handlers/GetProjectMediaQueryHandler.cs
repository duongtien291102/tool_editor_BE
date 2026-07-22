using AiVideoStudio.Application.Features.Media.DTOs;
using AiVideoStudio.Application.Features.Media.Queries;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Features.Media.Handlers;

public class GetProjectMediaQueryHandler : IRequestHandler<GetProjectMediaQuery, Result<MediaListResponse>>
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public GetProjectMediaQueryHandler(
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

    public async Task<Result<MediaListResponse>> Handle(GetProjectMediaQuery query, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result<MediaListResponse>.Failure(AuthErrors.Unauthorized);
        }

        var project = await _projectRepository.GetByIdAsync(query.ProjectId, cancellationToken);
        if (project == null || project.IsDeleted)
        {
            return Result<MediaListResponse>.Failure(MediaErrors.ProjectNotFound);
        }

        var isOwner = project.OwnerId == _currentUser.UserId;
        var isAdmin = _currentUser.Roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase) || r.Equals("Administrator", StringComparison.OrdinalIgnoreCase));

        if (!isOwner && !isAdmin)
        {
            return Result<MediaListResponse>.Failure(MediaErrors.UnauthorizedAccess);
        }

        var pagedResult = await _mediaAssetRepository.GetPagedByProjectIdAsync(
            query.ProjectId,
            query.Page,
            query.PageSize,
            query.Search,
            query.SortBy,
            query.SortDescending,
            query.AssetType,
            query.Status,
            cancellationToken
        );

        var dtos = _mapper.Map<IEnumerable<MediaDto>>(pagedResult.Items);
        var response = new MediaListResponse(
            dtos,
            pagedResult.TotalCount,
            pagedResult.Page,
            pagedResult.PageSize,
            pagedResult.TotalPages
        );

        return Result<MediaListResponse>.Success(response);
    }
}
