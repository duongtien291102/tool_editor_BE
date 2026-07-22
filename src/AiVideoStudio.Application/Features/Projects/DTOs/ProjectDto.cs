using AiVideoStudio.Domain.Enums;
using System;

namespace AiVideoStudio.Application.Features.Projects.DTOs;

public record ProjectDto(
    string Id,
    string Name,
    string? Description,
    string? Thumbnail,
    string OwnerId,
    ProjectStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
