using AiVideoStudio.Application.Features.Projects.DTOs;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Projects.Queries;

public record GetProjectsQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = false,
    ProjectStatus? Status = null
) : IRequest<Result<ProjectListResponse>>;
