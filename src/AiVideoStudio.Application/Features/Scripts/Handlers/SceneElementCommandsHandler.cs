using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Application.Features.Scripts.Commands;
using AiVideoStudio.Application.Features.Scripts.DTOs;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Domain.Base;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using AutoMapper;
using MediatR;

namespace AiVideoStudio.Application.Features.Scripts.Handlers;

public class SceneElementCommandsHandler :
    IRequestHandler<AddSceneElementCommand, Result<SceneElementDto>>,
    IRequestHandler<UpdateSceneElementCommand, Result<SceneElementDto>>,
    IRequestHandler<DeleteSceneElementCommand, Result<Unit>>,
    IRequestHandler<MoveSceneElementCommand, Result<Unit>>
{
    private readonly IScriptRepository _scriptRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public SceneElementCommandsHandler(
        IScriptRepository scriptRepository,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _scriptRepository = scriptRepository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    private bool IsAdmin() => _currentUser.Roles.Contains("Admin", StringComparer.OrdinalIgnoreCase) || 
                              _currentUser.Roles.Contains("Administrator", StringComparer.OrdinalIgnoreCase);

    public async Task<Result<SceneElementDto>> Handle(AddSceneElementCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            return Result<SceneElementDto>.Failure(ScriptErrors.Unauthorized);

        var script = await _scriptRepository.GetByIdAsync(request.ScriptId, cancellationToken);
        if (script == null)
            return Result<SceneElementDto>.Failure(ScriptErrors.NotFound);

        if (script.OwnerId != _currentUser.UserId && !IsAdmin())
            return Result<SceneElementDto>.Failure(ScriptErrors.Unauthorized);

        var element = script.AddSceneElement(request.SceneId, request.ElementType, request.Content, request.Metadata, _currentUser.UserId);

        try
        {
            await _scriptRepository.UpdateAsync(script, request.ExpectedVersion, cancellationToken);
        }
        catch (Exception ex) when (ex.Message.Contains("Concurrency") || ex.GetType().Name.Contains("Concurrency"))
        {
            return Result<SceneElementDto>.Failure(ScriptErrors.VersionConflict);
        }

        return Result<SceneElementDto>.Success(_mapper.Map<SceneElementDto>(element));
    }

    public async Task<Result<SceneElementDto>> Handle(UpdateSceneElementCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            return Result<SceneElementDto>.Failure(ScriptErrors.Unauthorized);

        var script = await _scriptRepository.GetByIdAsync(request.ScriptId, cancellationToken);
        if (script == null)
            return Result<SceneElementDto>.Failure(ScriptErrors.NotFound);

        if (script.OwnerId != _currentUser.UserId && !IsAdmin())
            return Result<SceneElementDto>.Failure(ScriptErrors.Unauthorized);

        var scene = script.Scenes.FirstOrDefault(s => s.Id == request.SceneId);
        if (scene == null)
            return Result<SceneElementDto>.Failure(ScriptErrors.NotFound);
            
        var element = scene.Elements.FirstOrDefault(e => e.Id == request.ElementId);
        if (element == null)
            return Result<SceneElementDto>.Failure(ScriptErrors.NotFound);

        script.UpdateSceneElement(request.SceneId, request.ElementId, request.Content, request.Metadata, _currentUser.UserId);

        try
        {
            await _scriptRepository.UpdateAsync(script, request.ExpectedVersion, cancellationToken);
        }
        catch (Exception ex) when (ex.Message.Contains("Concurrency") || ex.GetType().Name.Contains("Concurrency"))
        {
            return Result<SceneElementDto>.Failure(ScriptErrors.VersionConflict);
        }

        return Result<SceneElementDto>.Success(_mapper.Map<SceneElementDto>(element));
    }

    public async Task<Result<Unit>> Handle(DeleteSceneElementCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            return Result<Unit>.Failure(ScriptErrors.Unauthorized);

        var script = await _scriptRepository.GetByIdAsync(request.ScriptId, cancellationToken);
        if (script == null)
            return Result<Unit>.Failure(ScriptErrors.NotFound);

        if (script.OwnerId != _currentUser.UserId && !IsAdmin())
            return Result<Unit>.Failure(ScriptErrors.Unauthorized);

        script.RemoveSceneElement(request.SceneId, request.ElementId, _currentUser.UserId);

        try
        {
            await _scriptRepository.UpdateAsync(script, request.ExpectedVersion, cancellationToken);
        }
        catch (Exception ex) when (ex.Message.Contains("Concurrency") || ex.GetType().Name.Contains("Concurrency"))
        {
            return Result<Unit>.Failure(ScriptErrors.VersionConflict);
        }

        return Result<Unit>.Success(Unit.Value);
    }

    public async Task<Result<Unit>> Handle(MoveSceneElementCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            return Result<Unit>.Failure(ScriptErrors.Unauthorized);

        var script = await _scriptRepository.GetByIdAsync(request.ScriptId, cancellationToken);
        if (script == null)
            return Result<Unit>.Failure(ScriptErrors.NotFound);

        if (script.OwnerId != _currentUser.UserId && !IsAdmin())
            return Result<Unit>.Failure(ScriptErrors.Unauthorized);

        script.MoveSceneElement(request.SceneId, request.ElementId, request.NewOrder, _currentUser.UserId);

        try
        {
            await _scriptRepository.UpdateAsync(script, request.ExpectedVersion, cancellationToken);
        }
        catch (Exception ex) when (ex.Message.Contains("Concurrency") || ex.GetType().Name.Contains("Concurrency"))
        {
            return Result<Unit>.Failure(ScriptErrors.VersionConflict);
        }

        return Result<Unit>.Success(Unit.Value);
    }
}
