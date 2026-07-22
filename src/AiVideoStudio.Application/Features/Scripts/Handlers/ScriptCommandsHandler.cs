using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Application.Features.Scripts.Commands;
using AiVideoStudio.Application.Features.Scripts.DTOs;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Domain.Base;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using AutoMapper;
using MediatR;

namespace AiVideoStudio.Application.Features.Scripts.Handlers;

public class ScriptCommandsHandler :
    IRequestHandler<CreateScriptCommand, Result<ScriptDto>>,
    IRequestHandler<UpdateScriptCommand, Result<ScriptDto>>,
    IRequestHandler<AutoSaveScriptCommand, Result<ScriptDto>>,
    IRequestHandler<DeleteScriptCommand, Result<Unit>>
{
    private readonly IScriptRepository _scriptRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public ScriptCommandsHandler(
        IScriptRepository scriptRepository,
        IProjectRepository projectRepository,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _scriptRepository = scriptRepository;
        _projectRepository = projectRepository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    private bool IsAdmin() => _currentUser.Roles.Contains("Admin", StringComparer.OrdinalIgnoreCase) || 
                              _currentUser.Roles.Contains("Administrator", StringComparer.OrdinalIgnoreCase);

    public async Task<Result<ScriptDto>> Handle(CreateScriptCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            return Result<ScriptDto>.Failure(ScriptErrors.Unauthorized);

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
            return Result<ScriptDto>.Failure(new Error("Project.NotFound", "Project not found."));

        if (project.OwnerId != _currentUser.UserId && !IsAdmin())
            return Result<ScriptDto>.Failure(ScriptErrors.Unauthorized);

        var script = Script.Create(request.ProjectId, _currentUser.UserId, request.Name, request.Description);
        await _scriptRepository.AddAsync(script, cancellationToken);

        return Result<ScriptDto>.Success(_mapper.Map<ScriptDto>(script));
    }

    public async Task<Result<ScriptDto>> Handle(UpdateScriptCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            return Result<ScriptDto>.Failure(ScriptErrors.Unauthorized);

        var script = await _scriptRepository.GetByIdAsync(request.Id, cancellationToken);
        if (script == null)
            return Result<ScriptDto>.Failure(ScriptErrors.NotFound);

        if (script.OwnerId != _currentUser.UserId && !IsAdmin())
            return Result<ScriptDto>.Failure(ScriptErrors.Unauthorized);

        script.Rename(request.Name, request.Description, _currentUser.UserId);

        try
        {
            await _scriptRepository.UpdateAsync(script, request.ExpectedVersion, cancellationToken);
        }
        catch (Exception ex) when (ex.Message.Contains("Concurrency") || ex.GetType().Name.Contains("Concurrency"))
        {
            return Result<ScriptDto>.Failure(ScriptErrors.VersionConflict);
        }

        return Result<ScriptDto>.Success(_mapper.Map<ScriptDto>(script));
    }

    public async Task<Result<ScriptDto>> Handle(AutoSaveScriptCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            return Result<ScriptDto>.Failure(ScriptErrors.Unauthorized);

        var script = await _scriptRepository.GetByIdAsync(request.Id, cancellationToken);
        if (script == null)
            return Result<ScriptDto>.Failure(ScriptErrors.NotFound);

        if (script.OwnerId != _currentUser.UserId && !IsAdmin())
            return Result<ScriptDto>.Failure(ScriptErrors.Unauthorized);

        bool changed = false;
        if (script.Name != request.Name || script.Description != request.Description)
        {
            script.Rename(request.Name, request.Description, _currentUser.UserId);
            changed = true;
        }

        if (changed)
        {
            try
            {
                await _scriptRepository.UpdateAsync(script, request.ExpectedVersion, cancellationToken);
            }
            catch (Exception ex) when (ex.Message.Contains("Concurrency") || ex.GetType().Name.Contains("Concurrency"))
            {
                return Result<ScriptDto>.Failure(ScriptErrors.VersionConflict);
            }
        }

        return Result<ScriptDto>.Success(_mapper.Map<ScriptDto>(script));
    }

    public async Task<Result<Unit>> Handle(DeleteScriptCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            return Result<Unit>.Failure(ScriptErrors.Unauthorized);

        var script = await _scriptRepository.GetByIdAsync(request.Id, cancellationToken);
        if (script == null)
            return Result<Unit>.Failure(ScriptErrors.NotFound);

        if (script.OwnerId != _currentUser.UserId && !IsAdmin())
            return Result<Unit>.Failure(ScriptErrors.Unauthorized);

        script.SoftDelete(_currentUser.UserId);
        
        await _scriptRepository.SoftDeleteAsync(script, cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
