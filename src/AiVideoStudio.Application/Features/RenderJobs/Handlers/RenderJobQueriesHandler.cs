using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Application.Features.RenderJobs.DTOs;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using AutoMapper;
using MediatR;

namespace AiVideoStudio.Application.Features.RenderJobs.Handlers;

public class RenderJobQueriesHandler :
    IRequestHandler<GetRenderJobByIdQuery, Result<RenderJobDto>>,
    IRequestHandler<GetRenderJobsQuery, Result<PagedResult<RenderJobSummaryDto>>>,
    IRequestHandler<GetProjectRenderJobsQuery, Result<PagedResult<RenderJobSummaryDto>>>
{
    private readonly IRenderJobRepository _renderJobRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public RenderJobQueriesHandler(
        IRenderJobRepository renderJobRepository,
        IProjectRepository projectRepository,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _renderJobRepository = renderJobRepository;
        _projectRepository = projectRepository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<RenderJobDto>> Handle(GetRenderJobByIdQuery request, CancellationToken cancellationToken)
    {
        var job = await _renderJobRepository.GetByIdAsync(request.Id, cancellationToken);
        if (job == null)
            return Result<RenderJobDto>.Failure(RenderJobErrors.NotFound);

        if (!IsOwnerOrAdmin(job.OwnerId))
            return Result<RenderJobDto>.Failure(RenderJobErrors.Unauthorized);

        return Result<RenderJobDto>.Success(_mapper.Map<RenderJobDto>(job));
    }

    public async Task<Result<PagedResult<RenderJobSummaryDto>>> Handle(GetRenderJobsQuery request, CancellationToken cancellationToken)
    {
        var isAdmin = _currentUser.Roles.Contains("Admin") || _currentUser.Roles.Contains("Administrator");
        var ownerId = isAdmin ? null : _currentUser.UserId;

        var pagedJobs = await _renderJobRepository.GetPagedAsync(
            ownerId: ownerId,
            isAdmin: isAdmin,
            projectId: request.ProjectId,
            status: request.Status,
            provider: request.Provider,
            priority: request.Priority,
            search: request.Search,
            sortBy: request.SortBy,
            sortDescending: request.Descending,
            page: request.PageNumber,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        var dtos = pagedJobs.Items.Select(j => _mapper.Map<RenderJobSummaryDto>(j)).ToList();

        return Result<PagedResult<RenderJobSummaryDto>>.Success(
            new PagedResult<RenderJobSummaryDto>(dtos, pagedJobs.TotalCount, pagedJobs.Page, pagedJobs.PageSize));
    }

    public async Task<Result<PagedResult<RenderJobSummaryDto>>> Handle(GetProjectRenderJobsQuery request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
            return Result<PagedResult<RenderJobSummaryDto>>.Failure(RenderJobErrors.ProjectNotFound);

        if (!IsOwnerOrAdmin(project.OwnerId))
            return Result<PagedResult<RenderJobSummaryDto>>.Failure(RenderJobErrors.Unauthorized);

        var pagedJobs = await _renderJobRepository.GetByProjectIdPagedAsync(
            projectId: request.ProjectId,
            status: request.Status,
            sortBy: request.SortBy,
            sortDescending: request.Descending,
            page: request.PageNumber,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        var dtos = pagedJobs.Items.Select(j => _mapper.Map<RenderJobSummaryDto>(j)).ToList();

        return Result<PagedResult<RenderJobSummaryDto>>.Success(
            new PagedResult<RenderJobSummaryDto>(dtos, pagedJobs.TotalCount, pagedJobs.Page, pagedJobs.PageSize));
    }

    private bool IsOwnerOrAdmin(string ownerId)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId)) return false;
        return ownerId == _currentUser.UserId
               || _currentUser.Roles.Contains("Admin")
               || _currentUser.Roles.Contains("Administrator");
    }
}
