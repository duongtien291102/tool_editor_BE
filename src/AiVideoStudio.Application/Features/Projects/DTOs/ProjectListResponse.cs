using System.Collections.Generic;

namespace AiVideoStudio.Application.Features.Projects.DTOs;

public record ProjectListResponse(
    IEnumerable<ProjectDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
