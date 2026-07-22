using AiVideoStudio.Application.Features.Exports.DTOs;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Exports;

public record GetExportJobQuery(string Id) : IRequest<Result<ExportJobDto>>;

public record GetProjectExportJobsQuery(
    string ProjectId,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<ExportSummaryDto>>>;
