using System;
using AiVideoStudio.Domain.Base;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Application.Features.Scripts.DTOs;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Scripts.Commands;

// Script Commands
public record CreateScriptCommand(string ProjectId, string Name, string? Description) : IRequest<Result<ScriptDto>>;
public record UpdateScriptCommand(string Id, string Name, string? Description, int ExpectedVersion) : IRequest<Result<ScriptDto>>;
public record DeleteScriptCommand(string Id) : IRequest<Result<Unit>>;
public record AutoSaveScriptCommand(string Id, string Name, string? Description, int ExpectedVersion) : IRequest<Result<ScriptDto>>;

// Scene Commands
public record AddSceneCommand(string ScriptId, string Name, TimeSpan Duration, string? Notes, int ExpectedVersion) : IRequest<Result<SceneDto>>;
public record UpdateSceneCommand(string ScriptId, string SceneId, string Name, TimeSpan Duration, string? Notes, int ExpectedVersion) : IRequest<Result<SceneDto>>;
public record DeleteSceneCommand(string ScriptId, string SceneId, int ExpectedVersion) : IRequest<Result<Unit>>;
public record ReorderSceneCommand(string ScriptId, string SceneId, int NewOrder, int ExpectedVersion) : IRequest<Result<Unit>>;

// Scene Element Commands
public record AddSceneElementCommand(string ScriptId, string SceneId, ElementType ElementType, string? Content, string? Metadata, int ExpectedVersion) : IRequest<Result<SceneElementDto>>;
public record UpdateSceneElementCommand(string ScriptId, string SceneId, string ElementId, string? Content, string? Metadata, int ExpectedVersion) : IRequest<Result<SceneElementDto>>;
public record DeleteSceneElementCommand(string ScriptId, string SceneId, string ElementId, int ExpectedVersion) : IRequest<Result<Unit>>;
public record MoveSceneElementCommand(string ScriptId, string SceneId, string ElementId, int NewOrder, int ExpectedVersion) : IRequest<Result<Unit>>;
