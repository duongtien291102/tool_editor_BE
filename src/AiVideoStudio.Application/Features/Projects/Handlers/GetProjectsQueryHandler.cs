using AiVideoStudio.Application.Features.Projects.DTOs;
using AiVideoStudio.Application.Features.Projects.Queries;
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

namespace AiVideoStudio.Application.Features.Projects.Handlers;

public class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, Result<ProjectListResponse>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public GetProjectsQueryHandler(
        IProjectRepository projectRepository,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _projectRepository = projectRepository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<ProjectListResponse>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result<ProjectListResponse>.Failure(AuthErrors.Unauthorized);
        }

        var isAdmin = _currentUser.Roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase) || r.Equals("Administrator", StringComparison.OrdinalIgnoreCase));
        var ownerId = isAdmin ? null : _currentUser.UserId;

        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 && request.PageSize <= 100 ? request.PageSize : 10;

        var pagedResult = await _projectRepository.GetPagedAsync(
            ownerId,
            isAdmin,
            page,
            pageSize,
            request.Search,
            request.SortBy,
            request.SortDescending,
            request.Status,
            cancellationToken);

        var dtos = _mapper.Map<IEnumerable<ProjectDto>>(pagedResult.Items);
        var response = new ProjectListResponse(
            dtos,
            pagedResult.TotalCount,
            pagedResult.Page,
            pagedResult.PageSize,
            pagedResult.TotalPages);

        return Result<ProjectListResponse>.Success(response);
    }
}
