using System;
using System.Collections.Generic;

namespace AiVideoStudio.Application.Features.Media.DTOs;

public record MediaListResponse(
    IEnumerable<MediaDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
