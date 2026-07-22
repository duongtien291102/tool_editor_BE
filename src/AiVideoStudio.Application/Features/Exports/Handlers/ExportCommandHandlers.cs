using AiVideoStudio.Application.Features.Exports.DTOs;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Application.Interfaces.Export;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using AutoMapper;
using MediatR;

namespace AiVideoStudio.Application.Features.Exports.Handlers;

public sealed class ExportCommandHandlers :
    IRequestHandler<CreateExportJobCommand, Result<ExportJobDto>>,
    IRequestHandler<CancelExportJobCommand, Result>,
    IRequestHandler<RetryExportJobCommand, Result<ExportJobDto>>,
    IRequestHandler<UpdateExportProgressCommand, Result>
{
    private readonly IExportJobRepository _exports;
    private readonly IRenderJobRepository _renderJobs;
    private readonly IProjectRepository _projects;
    private readonly ITimelineRepository _timelines;
    private readonly IExportQueue _queue;
    private readonly IExportJobCanceller _canceller;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public ExportCommandHandlers(
        IExportJobRepository exports,
        IRenderJobRepository renderJobs,
        IProjectRepository projects,
        ITimelineRepository timelines,
        IExportQueue queue,
        IExportJobCanceller canceller,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _exports = exports;
        _renderJobs = renderJobs;
        _projects = projects;
        _timelines = timelines;
        _queue = queue;
        _canceller = canceller;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<ExportJobDto>> Handle(CreateExportJobCommand request, CancellationToken cancellationToken)
    {
        if (!IsAuthenticated()) return Result<ExportJobDto>.Failure(ExportErrors.Unauthorized);

        var project = await _projects.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null) return Result<ExportJobDto>.Failure(ExportErrors.ProjectNotFound);
        if (!CanAccess(project.OwnerId)) return Result<ExportJobDto>.Failure(ExportErrors.Forbidden);

        var renderJob = await _renderJobs.GetByIdAsync(request.RenderJobId, cancellationToken);
        if (renderJob is null || renderJob.ProjectId != request.ProjectId)
            return Result<ExportJobDto>.Failure(ExportErrors.RenderJobNotFound);

        var timeline = await _timelines.GetByIdAsync(request.TimelineId, cancellationToken);
        if (timeline is null || timeline.ProjectId != request.ProjectId)
            return Result<ExportJobDto>.Failure(ExportErrors.TimelineNotFound);

        var export = ExportJob.Create(
            request.RenderJobId,
            request.ProjectId,
            request.TimelineId,
            _currentUser.UserId!,
            timeline.Duration,
            $"{timeline.ResolutionWidth}x{timeline.ResolutionHeight}",
            timeline.FrameRate,
            request.VideoCodec,
            request.AudioCodec,
            request.Container,
            request.MaxRetryCount);

        await _exports.AddAsync(export, cancellationToken);
        await _queue.EnqueueAsync(new ExportQueueItem(export.Id, export.CreatedAt), cancellationToken);
        return Result<ExportJobDto>.Success(_mapper.Map<ExportJobDto>(export));
    }

    public async Task<Result> Handle(CancelExportJobCommand request, CancellationToken cancellationToken)
    {
        var export = await _exports.GetByIdAsync(request.ExportJobId, cancellationToken);
        if (export is null) return Result.Failure(ExportErrors.NotFound);
        if (!IsAuthenticated()) return Result.Failure(ExportErrors.Unauthorized);
        if (!CanAccess(export.OwnerId)) return Result.Failure(ExportErrors.Forbidden);

        try
        {
            export.Cancel(_currentUser.UserId!);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure(ExportErrors.InvalidTransition);
        }

        _queue.Remove(export.Id);
        _canceller.CancelActiveExport(export.Id);
        await _exports.UpdateAsync(export, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<ExportJobDto>> Handle(RetryExportJobCommand request, CancellationToken cancellationToken)
    {
        var export = await _exports.GetByIdAsync(request.ExportJobId, cancellationToken);
        if (export is null) return Result<ExportJobDto>.Failure(ExportErrors.NotFound);
        if (!IsAuthenticated()) return Result<ExportJobDto>.Failure(ExportErrors.Unauthorized);
        if (!CanAccess(export.OwnerId)) return Result<ExportJobDto>.Failure(ExportErrors.Forbidden);

        try
        {
            export.Retry(_currentUser.UserId!);
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("Maximum retry", StringComparison.Ordinal))
        {
            return Result<ExportJobDto>.Failure(ExportErrors.MaxRetriesReached);
        }
        catch (InvalidOperationException)
        {
            return Result<ExportJobDto>.Failure(ExportErrors.InvalidTransition);
        }

        await _exports.UpdateAsync(export, cancellationToken);
        await _queue.EnqueueAsync(new ExportQueueItem(export.Id, export.CreatedAt), cancellationToken);
        return Result<ExportJobDto>.Success(_mapper.Map<ExportJobDto>(export));
    }

    public async Task<Result> Handle(UpdateExportProgressCommand request, CancellationToken cancellationToken)
    {
        var export = await _exports.GetByIdAsync(request.ExportJobId, cancellationToken);
        if (export is null) return Result.Failure(ExportErrors.NotFound);

        try
        {
            export.UpdateProgress(request.Progress);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return Result.Failure(ExportErrors.InvalidTransition);
        }

        await _exports.UpdateAsync(export, cancellationToken);
        return Result.Success();
    }

    private bool IsAuthenticated() => _currentUser.IsAuthenticated && !string.IsNullOrWhiteSpace(_currentUser.UserId);

    private bool CanAccess(string ownerId) =>
        ownerId == _currentUser.UserId ||
        _currentUser.Roles.Contains("Admin") ||
        _currentUser.Roles.Contains("Administrator");
}
