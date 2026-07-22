using AiVideoStudio.Application.Features.Uploads.DTOs;
using AiVideoStudio.Shared.Responses;
using MediatR;
namespace AiVideoStudio.Application.Features.Uploads;
public record GetUploadSessionQuery(string Id) : IRequest<Result<UploadSessionDto>>;
public record GetProjectUploadsQuery(string ProjectId, int Page = 1, int PageSize = 20) : IRequest<Result<PagedResult<UploadSummaryDto>>>;
