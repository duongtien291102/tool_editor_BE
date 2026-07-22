using System;
using System.Collections.Generic;
using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Application.Features.Scripts.DTOs;

public record ScriptDto(
    string Id,
    string ProjectId,
    string Name,
    string? Description,
    string OwnerId,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    List<SceneDto> Scenes);

public record ScriptSummaryDto(
    string Id,
    string ProjectId,
    string Name,
    string? Description,
    string OwnerId,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record SceneDto(
    string Id,
    string Name,
    TimeSpan Duration,
    string? Notes,
    int Order,
    List<SceneElementDto> Elements);

public record SceneElementDto(
    string Id,
    ElementType ElementType,
    string? Content,
    string? Metadata,
    int Order);
