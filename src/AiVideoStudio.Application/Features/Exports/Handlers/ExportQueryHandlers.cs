using AiVideoStudio.Application.Features.Exports.DTOs;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using AutoMapper;
using MediatR;

namespace AiVideoStudio.Application.Features.Exports.Handlers;

public sealed class ExportQueryHandlers :
    IRequestHandler<GetExportJobQuery, Result<ExportJobDto>>,
    IRequestHandler<GetProjectExportJobsQuery, Result<PagedResult<ExportSummaryDto>>>
{
    private readonly IExportJobRepository _exports;
    private readonly IProjectRepository _projects;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public ExportQueryHandlers(
        IExportJobRepository exports,
        IProjectRepository projects,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _exports = exports;
        _projects = projects;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<ExportJobDto>> Handle(GetExportJobQuery request, CancellationToken cancellationToken)
    {
        var export = await _exports.GetByIdAsync(request.Id, cancellationToken);
        if (export is null) return Result<ExportJobDto>.Failure(ExportErrors.NotFound);
        if (!IsAuthenticated()) return Result<ExportJobDto>.Failure(ExportErrors.Unauthorized);
        if (!CanAccess(export.OwnerId)) return Result<ExportJobDto>.Failure(ExportErrors.Forbidden);
        return Result<ExportJobDto>.Success(_mapper.Map<ExportJobDto>(export));
    }

    public async Task<Result<PagedResult<ExportSummaryDto>>> Handle(
        GetProjectExportJobsQuery request,
        CancellationToken cancellationToken)
    {
        if (!IsAuthenticated()) return Result<PagedResult<ExportSummaryDto>>.Failure(ExportErrors.Unauthorized);
        var project = await _projects.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null) return Result<PagedResult<ExportSummaryDto>>.Failure(ExportErrors.ProjectNotFound);
        if (!CanAccess(project.OwnerId)) return Result<PagedResult<ExportSummaryDto>>.Failure(ExportErrors.Forbidden);

        var page = await _exports.GetByProjectIdPagedAsync(
            request.ProjectId,
            request.Page,
            request.PageSize,
            cancellationToken);
        var items = page.Items.Select(_mapper.Map<ExportSummaryDto>).ToList();
        return Result<PagedResult<ExportSummaryDto>>.Success(
            new PagedResult<ExportSummaryDto>(items, page.TotalCount, page.Page, page.PageSize));
    }

    private bool IsAuthenticated() => _currentUser.IsAuthenticated && !string.IsNullOrWhiteSpace(_currentUser.UserId);

    private bool CanAccess(string ownerId) =>
        ownerId == _currentUser.UserId ||
        _currentUser.Roles.Contains("Admin") ||
        _currentUser.Roles.Contains("Administrator");
}
