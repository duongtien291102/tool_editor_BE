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

public class SceneCommandsHandler :
    IRequestHandler<AddSceneCommand, Result<SceneDto>>,
    IRequestHandler<UpdateSceneCommand, Result<SceneDto>>,
    IRequestHandler<DeleteSceneCommand, Result<Unit>>,
    IRequestHandler<ReorderSceneCommand, Result<Unit>>
{
    private readonly IScriptRepository _scriptRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public SceneCommandsHandler(
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

    public async Task<Result<SceneDto>> Handle(AddSceneCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            return Result<SceneDto>.Failure(ScriptErrors.Unauthorized);

        var script = await _scriptRepository.GetByIdAsync(request.ScriptId, cancellationToken);
        if (script == null)
            return Result<SceneDto>.Failure(ScriptErrors.NotFound);

        if (script.OwnerId != _currentUser.UserId && !IsAdmin())
            return Result<SceneDto>.Failure(ScriptErrors.Unauthorized);

        var scene = script.AddScene(request.Name, request.Duration, request.Notes, _currentUser.UserId);

        try
        {
            await _scriptRepository.UpdateAsync(script, request.ExpectedVersion, cancellationToken);
        }
        catch (Exception ex) when (ex.Message.Contains("Concurrency") || ex.GetType().Name.Contains("Concurrency"))
        {
            return Result<SceneDto>.Failure(ScriptErrors.VersionConflict);
        }

        return Result<SceneDto>.Success(_mapper.Map<SceneDto>(scene));
    }

    public async Task<Result<SceneDto>> Handle(UpdateSceneCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            return Result<SceneDto>.Failure(ScriptErrors.Unauthorized);

        var script = await _scriptRepository.GetByIdAsync(request.ScriptId, cancellationToken);
        if (script == null)
            return Result<SceneDto>.Failure(ScriptErrors.NotFound);

        if (script.OwnerId != _currentUser.UserId && !IsAdmin())
            return Result<SceneDto>.Failure(ScriptErrors.Unauthorized);

        var scene = script.Scenes.FirstOrDefault(s => s.Id == request.SceneId);
        if (scene == null)
            return Result<SceneDto>.Failure(ScriptErrors.NotFound);

        script.UpdateScene(request.SceneId, request.Name, request.Duration, request.Notes, _currentUser.UserId);

        try
        {
            await _scriptRepository.UpdateAsync(script, request.ExpectedVersion, cancellationToken);
        }
        catch (Exception ex) when (ex.Message.Contains("Concurrency") || ex.GetType().Name.Contains("Concurrency"))
        {
            return Result<SceneDto>.Failure(ScriptErrors.VersionConflict);
        }

        return Result<SceneDto>.Success(_mapper.Map<SceneDto>(scene));
    }

    public async Task<Result<Unit>> Handle(DeleteSceneCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            return Result<Unit>.Failure(ScriptErrors.Unauthorized);

        var script = await _scriptRepository.GetByIdAsync(request.ScriptId, cancellationToken);
        if (script == null)
            return Result<Unit>.Failure(ScriptErrors.NotFound);

        if (script.OwnerId != _currentUser.UserId && !IsAdmin())
            return Result<Unit>.Failure(ScriptErrors.Unauthorized);

        script.RemoveScene(request.SceneId, _currentUser.UserId);

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

    public async Task<Result<Unit>> Handle(ReorderSceneCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            return Result<Unit>.Failure(ScriptErrors.Unauthorized);

        var script = await _scriptRepository.GetByIdAsync(request.ScriptId, cancellationToken);
        if (script == null)
            return Result<Unit>.Failure(ScriptErrors.NotFound);

        if (script.OwnerId != _currentUser.UserId && !IsAdmin())
            return Result<Unit>.Failure(ScriptErrors.Unauthorized);

        script.ReorderScene(request.SceneId, request.NewOrder, _currentUser.UserId);

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
