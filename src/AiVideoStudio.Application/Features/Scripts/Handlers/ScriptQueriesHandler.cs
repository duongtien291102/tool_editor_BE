using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Application.Features.Scripts.DTOs;
using AiVideoStudio.Application.Features.Scripts.Queries;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using AutoMapper;
using MediatR;

namespace AiVideoStudio.Application.Features.Scripts.Handlers;

public class ScriptQueriesHandler :
    IRequestHandler<GetScriptByIdQuery, Result<ScriptDto>>,
    IRequestHandler<GetScriptsByProjectQuery, Result<PagedResult<ScriptSummaryDto>>>
{
    private readonly IScriptRepository _scriptRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public ScriptQueriesHandler(
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

    public async Task<Result<ScriptDto>> Handle(GetScriptByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            return Result<ScriptDto>.Failure(ScriptErrors.Unauthorized);

        var script = await _scriptRepository.GetByIdAsync(request.Id, cancellationToken);
        if (script == null)
            return Result<ScriptDto>.Failure(ScriptErrors.NotFound);

        if (script.OwnerId != _currentUser.UserId && !IsAdmin())
        {
            var project = await _projectRepository.GetByIdAsync(script.ProjectId, cancellationToken);
            if (project == null || project.OwnerId != _currentUser.UserId)
            {
                return Result<ScriptDto>.Failure(ScriptErrors.Unauthorized);
            }
        }

        return Result<ScriptDto>.Success(_mapper.Map<ScriptDto>(script));
    }

    public async Task<Result<PagedResult<ScriptSummaryDto>>> Handle(GetScriptsByProjectQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
            return Result<PagedResult<ScriptSummaryDto>>.Failure(ScriptErrors.Unauthorized);

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
            return Result<PagedResult<ScriptSummaryDto>>.Failure(new Error("Project.NotFound", "Project not found."));

        if (project.OwnerId != _currentUser.UserId && !IsAdmin())
            return Result<PagedResult<ScriptSummaryDto>>.Failure(ScriptErrors.Unauthorized);

        var pagedScripts = await _scriptRepository.GetScriptsByProjectAsync(
            request.ProjectId,
            request.SearchTerm,
            request.IncludeDeleted,
            request.SortBy,
            request.Descending,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var summaries = pagedScripts.Items.Select(s => _mapper.Map<ScriptSummaryDto>(s)).ToList();

        var result = new PagedResult<ScriptSummaryDto>(summaries, pagedScripts.TotalCount, pagedScripts.Page, pagedScripts.PageSize);
        return Result<PagedResult<ScriptSummaryDto>>.Success(result);
    }
}
