using AiVideoStudio.Domain.Base;
using AiVideoStudio.Application.Features.Scripts.DTOs;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Scripts.Queries;

public record GetScriptByIdQuery(string Id) : IRequest<Result<ScriptDto>>;

public record GetScriptsByProjectQuery(
    string ProjectId,
    string? SearchTerm,
    bool IncludeDeleted,
    string? SortBy,
    bool Descending,
    int PageNumber,
    int PageSize) : IRequest<Result<PagedResult<ScriptSummaryDto>>>;
