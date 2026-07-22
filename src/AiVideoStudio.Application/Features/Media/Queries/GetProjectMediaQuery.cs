using AiVideoStudio.Application.Features.Media.DTOs;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Media.Queries;

public record GetProjectMediaQuery(
    string ProjectId,
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = false,
    AssetType? AssetType = null,
    MediaStatus? Status = null
) : IRequest<Result<MediaListResponse>>;
