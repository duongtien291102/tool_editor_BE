using System;
using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Application.Features.RenderJobs.DTOs;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Application.Interfaces.Render;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using AutoMapper;
using MediatR;

namespace AiVideoStudio.Application.Features.RenderJobs.Handlers;

public class RenderJobCommandsHandler :
    IRequestHandler<CreateRenderJobCommand, Result<RenderJobDto>>,
    IRequestHandler<CancelRenderJobCommand, Result>,
    IRequestHandler<RetryRenderJobCommand, Result<RenderJobDto>>,
    IRequestHandler<UpdateRenderProgressCommand, Result>
{
    private readonly IRenderJobRepository _renderJobRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IRenderQueue _renderQueue;
    private readonly IRenderJobCanceller _canceller;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public RenderJobCommandsHandler(
        IRenderJobRepository renderJobRepository,
        IProjectRepository projectRepository,
        IRenderQueue renderQueue,
        IRenderJobCanceller canceller,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _renderJobRepository = renderJobRepository;
        _projectRepository = projectRepository;
        _renderQueue = renderQueue;
        _canceller = canceller;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Create
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<Result<RenderJobDto>> Handle(CreateRenderJobCommand request, CancellationToken cancellationToken)
    {
        // Verify project exists
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
            return Result<RenderJobDto>.Failure(RenderJobErrors.ProjectNotFound);

        var userId = _currentUser.UserId;
        if (!IsOwnerOrAdmin(project.OwnerId, userId))
            return Result<RenderJobDto>.Failure(RenderJobErrors.Unauthorized);

        // Create aggregate
        var job = RenderJob.Create(
            projectId: request.ProjectId,
            ownerId: userId!,
            jobType: request.JobType,
            provider: request.Provider,
            priority: request.Priority,
            maxRetryCount: request.MaxRetryCount,
            timelineId: request.TimelineId,
            scriptId: request.ScriptId,
            inputPayload: request.InputPayload);

        // Transition: Pending → Queued
        job.Queue();

        await _renderJobRepository.AddAsync(job, cancellationToken);

        // Enqueue the lightweight QueueItem (not the full aggregate)
        var queueItem = new QueueItem(job.Id, job.Priority, job.CreatedAt);
        await _renderQueue.EnqueueAsync(queueItem, cancellationToken);

        return Result<RenderJobDto>.Success(_mapper.Map<RenderJobDto>(job));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Cancel
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<Result> Handle(CancelRenderJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _renderJobRepository.GetByIdAsync(request.JobId, cancellationToken);
        if (job == null)
            return Result.Failure(RenderJobErrors.NotFound);

        if (!IsOwnerOrAdmin(job.OwnerId, _currentUser.UserId))
            return Result.Failure(RenderJobErrors.Unauthorized);

        try
        {
            job.Cancel(_currentUser.UserId!);
        }
        catch (InvalidOperationException)
        {
            return DetermineTransitionError(job);
        }

        // Remove from queue if it's pending/queued
        await _renderQueue.RemoveAsync(job.Id, cancellationToken);
        
        // Cancel active processing if running
        _canceller.CancelActiveJob(job.Id);

        await _renderJobRepository.UpdateAsync(job, cancellationToken);
        return Result.Success();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Retry
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<Result<RenderJobDto>> Handle(RetryRenderJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _renderJobRepository.GetByIdAsync(request.JobId, cancellationToken);
        if (job == null)
            return Result<RenderJobDto>.Failure(RenderJobErrors.NotFound);

        if (!IsOwnerOrAdmin(job.OwnerId, _currentUser.UserId))
            return Result<RenderJobDto>.Failure(RenderJobErrors.Unauthorized);

        try
        {
            job.Retry(_currentUser.UserId!);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Max retry count"))
        {
            return Result<RenderJobDto>.Failure(RenderJobErrors.MaxRetriesReached);
        }
        catch (InvalidOperationException)
        {
            return Result<RenderJobDto>.Failure(RenderJobErrors.CannotRetry);
        }

        await _renderJobRepository.UpdateAsync(job, cancellationToken);

        // Re-enqueue
        var queueItem = new QueueItem(job.Id, job.Priority, job.CreatedAt);
        await _renderQueue.EnqueueAsync(queueItem, cancellationToken);

        return Result<RenderJobDto>.Success(_mapper.Map<RenderJobDto>(job));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateProgress (internal — used by RenderWorker)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<Result> Handle(UpdateRenderProgressCommand request, CancellationToken cancellationToken)
    {
        var job = await _renderJobRepository.GetByIdAsync(request.JobId, cancellationToken);
        if (job == null)
            return Result.Failure(RenderJobErrors.NotFound);

        try
        {
            job.UpdateProgress(request.Progress);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Result.Failure(RenderJobErrors.InvalidProgress);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure(RenderJobErrors.InvalidStatusTransition);
        }

        await _renderJobRepository.UpdateAsync(job, cancellationToken);
        return Result.Success();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private bool IsOwnerOrAdmin(string ownerId, string? userId)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        return ownerId == userId
               || _currentUser.Roles.Contains("Admin")
               || _currentUser.Roles.Contains("Administrator");
    }

    private static Result DetermineTransitionError(RenderJob job)
    {
        return job.Status switch
        {
            Domain.Enums.RenderJobStatus.Completed => Result.Failure(RenderJobErrors.AlreadyCompleted),
            Domain.Enums.RenderJobStatus.Cancelled => Result.Failure(RenderJobErrors.AlreadyCancelled),
            _ => Result.Failure(RenderJobErrors.InvalidStatusTransition)
        };
    }
}
