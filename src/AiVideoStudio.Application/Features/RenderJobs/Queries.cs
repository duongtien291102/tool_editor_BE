using AiVideoStudio.Application.Features.RenderJobs.DTOs;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.RenderJobs;

// ─────────────────────────────────────────────────────────────────────────────
// Queries
// ─────────────────────────────────────────────────────────────────────────────

public record GetRenderJobByIdQuery(string Id) : IRequest<Result<RenderJobDto>>;

public record GetRenderJobsQuery(
    string? ProjectId,
    string? Status,
    string? Provider,
    string? Priority,
    string? Search,
    string? SortBy,
    bool Descending,
    int PageNumber,
    int PageSize
) : IRequest<Result<PagedResult<RenderJobSummaryDto>>>;

public record GetProjectRenderJobsQuery(
    string ProjectId,
    string? Status,
    string? SortBy,
    bool Descending,
    int PageNumber,
    int PageSize
) : IRequest<Result<PagedResult<RenderJobSummaryDto>>>;
