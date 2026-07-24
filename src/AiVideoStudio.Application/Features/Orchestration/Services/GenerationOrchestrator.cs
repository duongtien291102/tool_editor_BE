using System.Collections.Concurrent;
using System.Diagnostics;
using AiVideoStudio.Application.Interfaces.Orchestration;
using AiVideoStudio.Domain.Entities.Orchestration;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces.Orchestration;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Logging;
using AiVideoStudio.Shared.Responses;

namespace AiVideoStudio.Application.Features.Orchestration.Services;

public sealed class GenerationOrchestrator : IGenerationOrchestrator
{
    private readonly IGenerationWorkflowRepository _repository;
    private readonly IWorkflowSchedulerEngine _scheduler;
    private readonly IOrchestrationDispatcher _dispatcher;
    private readonly IAppLogger<GenerationOrchestrator> _logger;

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeExecutions = new();

    public GenerationOrchestrator(
        IGenerationWorkflowRepository repository,
        IWorkflowSchedulerEngine scheduler,
        IOrchestrationDispatcher dispatcher,
        IAppLogger<GenerationOrchestrator> logger)
    {
        _repository = repository;
        _scheduler = scheduler;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task<Result<GenerationWorkflow>> CreateWorkflowAsync(
        string projectId,
        string ownerId,
        string name,
        string? description,
        IEnumerable<OrchestrationStep> steps,
        WorkflowPolicy? policy = null,
        WorkflowExecutionContext? context = null,
        string? sceneId = null,
        string? shotId = null,
        CancellationToken ct = default)
    {
        try
        {
            var workflow = GenerationWorkflow.Create(
                projectId,
                ownerId,
                name,
                description,
                steps,
                policy,
                context,
                sceneId,
                shotId);

            await _repository.AddAsync(workflow, ct);

            _logger.LogInformation(
                2001,
                $"Created Generation Workflow {workflow.Id} for Project {projectId} with {workflow.Steps.Count} steps.");

            return Result<GenerationWorkflow>.Success(workflow);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(2002, $"Failed to create workflow due to DAG/Validation error: {ex.Message}");
            return Result<GenerationWorkflow>.Failure(WorkflowErrors.InvalidGraph);
        }
        catch (Exception ex)
        {
            _logger.LogError(2003, ex, "Unexpected error creating generation workflow.");
            return Result<GenerationWorkflow>.Failure(new Error("Workflow.CreateError", ex.Message));
        }
    }

    public async Task<Result<GenerationWorkflow>> QueueWorkflowAsync(string workflowId, CancellationToken ct = default)
    {
        var workflow = await _repository.GetByIdAsync(workflowId, ct);
        if (workflow is null) return Result<GenerationWorkflow>.Failure(WorkflowErrors.NotFound);

        try
        {
            workflow.Queue();
            await _repository.UpdateAsync(workflow, ct);
            return Result<GenerationWorkflow>.Success(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(2004, ex, $"Failed to queue workflow {workflowId}.");
            return Result<GenerationWorkflow>.Failure(WorkflowErrors.InvalidState);
        }
    }

    public async Task<Result<WorkflowResult>> ExecuteWorkflowAsync(string workflowId, CancellationToken ct = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (!_activeExecutions.TryAdd(workflowId, linkedCts))
        {
            return Result<WorkflowResult>.Failure(new Error("Workflow.AlreadyExecuting", "Workflow is already being executed."));
        }

        var stopwatch = Stopwatch.StartNew();
        int batchCount = 0;
        int retryCount = 0;

        try
        {
            var workflow = await _repository.GetByIdAsync(workflowId, linkedCts.Token);
            if (workflow is null) return Result<WorkflowResult>.Failure(WorkflowErrors.NotFound);

            if (workflow.State != WorkflowState.Queued && workflow.State != WorkflowState.Draft)
            {
                return Result<WorkflowResult>.Failure(WorkflowErrors.InvalidState);
            }

            var executionId = Guid.NewGuid().ToString();
            workflow.Start(executionId);
            await _repository.UpdateAsync(workflow, linkedCts.Token);

            while (!linkedCts.Token.IsCancellationRequested)
            {
                workflow = await _repository.GetByIdAsync(workflowId, linkedCts.Token)
                           ?? throw new InvalidOperationException("Workflow disappeared during execution.");

                if (workflow.State == WorkflowState.Cancelled)
                {
                    stopwatch.Stop();
                    return Result<WorkflowResult>.Failure(new Error("Workflow.Cancelled", "Workflow execution cancelled."));
                }

                var readySteps = _scheduler.GetReadySteps(workflow);
                if (readySteps.Count == 0)
                {
                    if (workflow.Steps.All(x => x.Status is WorkflowStepStatus.Completed or WorkflowStepStatus.Skipped))
                    {
                        workflow.Complete();
                        await _repository.UpdateAsync(workflow, linkedCts.Token);

                        stopwatch.Stop();
                        var res = WorkflowResult.Success(
                            workflowId,
                            workflow.State,
                            stopwatch.Elapsed,
                            workflow.Steps.Count,
                            workflow.Steps.Count(s => s.Status == WorkflowStepStatus.Completed),
                            workflow.Steps.Count(s => s.Status == WorkflowStepStatus.Failed),
                            workflow.Steps.Count(s => s.Status == WorkflowStepStatus.Skipped),
                            batchCount,
                            retryCount,
                            workflow.Context.Data);

                        return Result<WorkflowResult>.Success(res);
                    }

                    if (workflow.Steps.Any(x => x.Status == WorkflowStepStatus.Failed))
                    {
                        if (workflow.Policy.ContinueOnFailure)
                        {
                            workflow.PartialComplete("Some steps failed but policy allowed partial completion.");
                            await _repository.UpdateAsync(workflow, linkedCts.Token);

                            stopwatch.Stop();
                            return Result<WorkflowResult>.Success(WorkflowResult.Success(
                                workflowId,
                                workflow.State,
                                stopwatch.Elapsed,
                                workflow.Steps.Count,
                                workflow.Steps.Count(s => s.Status == WorkflowStepStatus.Completed),
                                workflow.Steps.Count(s => s.Status == WorkflowStepStatus.Failed),
                                workflow.Steps.Count(s => s.Status == WorkflowStepStatus.Skipped),
                                batchCount,
                                retryCount,
                                workflow.Context.Data));
                        }

                        stopwatch.Stop();
                        return Result<WorkflowResult>.Failure(new Error("Workflow.Failed", workflow.Error ?? "Workflow step failed."));
                    }

                    throw new InvalidOperationException("Workflow execution stuck in non-advancing state.");
                }

                // Batch ready steps according to policy
                var batches = _scheduler.ScheduleBatches(readySteps, workflow.Policy.BatchSize);
                batchCount += batches.Count;

                using var semaphore = new SemaphoreSlim(workflow.Policy.Parallelism);
                var tasks = new List<Task>();

                foreach (var batch in batches)
                {
                    await semaphore.WaitAsync(linkedCts.Token);

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            foreach (var step in batch.Steps)
                            {
                                linkedCts.Token.ThrowIfCancellationRequested();

                                bool success = false;
                                string? lastError = null;

                                while (!success)
                                {
                                    if (step.Status != WorkflowStepStatus.Running)
                                    {
                                        workflow.StepStarted(step.Id);
                                        await _repository.UpdateAsync(workflow, linkedCts.Token);
                                    }

                                    try
                                    {
                                        using var stepTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token);
                                        stepTimeoutCts.CancelAfter(TimeSpan.FromSeconds(step.TimeoutSeconds));

                                        var output = await _dispatcher.DispatchStepAsync(workflow, step, workflow.Context, stepTimeoutCts.Token);

                                        workflow.StepCompleted(step.Id, output);
                                        await _repository.UpdateAsync(workflow, linkedCts.Token);
                                        success = true;
                                    }
                                    catch (Exception ex)
                                    {
                                        lastError = ex.Message;
                                        workflow.StepFailed(step.Id, lastError);
                                        await _repository.UpdateAsync(workflow, linkedCts.Token);

                                        if (step.CanRetry())
                                        {
                                            step.Retry();
                                            retryCount++;
                                            await _repository.UpdateAsync(workflow, linkedCts.Token);
                                            _logger.LogWarning(2005, $"Retrying step {step.Id} (Attempt {step.RetryCount}/{step.MaxRetries})");
                                            continue;
                                        }

                                        // Fallback provider attempt if specified
                                        if (!string.IsNullOrEmpty(workflow.Policy.ProviderFallback) && step.Provider != workflow.Policy.ProviderFallback)
                                        {
                                            _logger.LogWarning(2006, $"Switching step {step.Id} to fallback provider '{workflow.Policy.ProviderFallback}'");
                                            step.SetFallbackProvider(workflow.Policy.ProviderFallback);
                                            step.Reset();
                                            await _repository.UpdateAsync(workflow, linkedCts.Token);
                                            continue;
                                        }

                                        // Compensation & Rollback
                                        await _dispatcher.ExecuteCompensationAsync(workflow, step, lastError, linkedCts.Token);
                                        await _repository.UpdateAsync(workflow, linkedCts.Token);

                                        if (!workflow.Policy.ContinueOnFailure)
                                        {
                                            workflow.Fail($"Step '{step.Name}' failed after retries: {lastError}");
                                            await _repository.UpdateAsync(workflow, linkedCts.Token);
                                        }
                                        else
                                        {
                                            step.Skip();
                                            await _repository.UpdateAsync(workflow, linkedCts.Token);
                                        }

                                        break;
                                    }
                                }

                                if (!success && !workflow.Policy.ContinueOnFailure)
                                {
                                    break;
                                }
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, linkedCts.Token));
                }

                await Task.WhenAll(tasks);
            }

            stopwatch.Stop();
            return Result<WorkflowResult>.Failure(new Error("Workflow.Cancelled", "Workflow cancelled."));
        }
        catch (OperationCanceledException)
        {
            var workflow = await _repository.GetByIdAsync(workflowId, CancellationToken.None);
            if (workflow is not null && workflow.State != WorkflowState.Cancelled)
            {
                workflow.Cancel("Operation cancelled.");
                await _repository.UpdateAsync(workflow, CancellationToken.None);
            }

            stopwatch.Stop();
            return Result<WorkflowResult>.Failure(new Error("Workflow.Cancelled", "Workflow cancelled."));
        }
        catch (Exception ex)
        {
            _logger.LogError(2007, ex, $"Unexpected error executing workflow {workflowId}.");
            stopwatch.Stop();

            var workflow = await _repository.GetByIdAsync(workflowId, CancellationToken.None);
            if (workflow is not null && workflow.State == WorkflowState.Running)
            {
                workflow.Fail(ex.Message);
                await _repository.UpdateAsync(workflow, CancellationToken.None);
            }

            return Result<WorkflowResult>.Failure(new Error("Workflow.ExecutionError", ex.Message));
        }
        finally
        {
            _activeExecutions.TryRemove(workflowId, out _);
        }
    }

    public async Task<Result<GenerationWorkflow>> RetryWorkflowAsync(string workflowId, string? stepId = null, CancellationToken ct = default)
    {
        var workflow = await _repository.GetByIdAsync(workflowId, ct);
        if (workflow is null) return Result<GenerationWorkflow>.Failure(WorkflowErrors.NotFound);

        try
        {
            workflow.Retry();
            await _repository.UpdateAsync(workflow, ct);
            return Result<GenerationWorkflow>.Success(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(2008, ex, $"Failed to retry workflow {workflowId}.");
            return Result<GenerationWorkflow>.Failure(WorkflowErrors.InvalidState);
        }
    }

    public async Task<Result<GenerationWorkflow>> ResumeWorkflowAsync(string workflowId, CancellationToken ct = default)
    {
        var workflow = await _repository.GetByIdAsync(workflowId, ct);
        if (workflow is null) return Result<GenerationWorkflow>.Failure(WorkflowErrors.NotFound);

        try
        {
            workflow.Resume();
            await _repository.UpdateAsync(workflow, ct);
            return Result<GenerationWorkflow>.Success(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(2009, ex, $"Failed to resume workflow {workflowId}.");
            return Result<GenerationWorkflow>.Failure(WorkflowErrors.InvalidState);
        }
    }

    public async Task<Result> CancelWorkflowAsync(string workflowId, string reason = "User requested cancellation.", CancellationToken ct = default)
    {
        if (_activeExecutions.TryGetValue(workflowId, out var cts))
        {
            cts.Cancel();
        }

        var workflow = await _repository.GetByIdAsync(workflowId, ct);
        if (workflow is null) return Result.Failure(WorkflowErrors.NotFound);

        try
        {
            if (workflow.State != WorkflowState.Cancelled)
            {
                workflow.Cancel(reason);
                await _repository.UpdateAsync(workflow, ct);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(2010, ex, $"Failed to cancel workflow {workflowId}.");
            return Result.Failure(WorkflowErrors.InvalidState);
        }
    }

    public async Task<Result<GenerationWorkflow>> GetWorkflowAsync(string workflowId, CancellationToken ct = default)
    {
        var workflow = await _repository.GetByIdAsync(workflowId, ct);
        return workflow is null
            ? Result<GenerationWorkflow>.Failure(WorkflowErrors.NotFound)
            : Result<GenerationWorkflow>.Success(workflow);
    }

    public async Task<Result<WorkflowState>> GetWorkflowStatusAsync(string workflowId, CancellationToken ct = default)
    {
        var workflow = await _repository.GetByIdAsync(workflowId, ct);
        return workflow is null
            ? Result<WorkflowState>.Failure(WorkflowErrors.NotFound)
            : Result<WorkflowState>.Success(workflow.State);
    }

    public async Task<Result<IReadOnlyList<WorkflowHistory>>> GetWorkflowHistoryAsync(string workflowId, CancellationToken ct = default)
    {
        var workflow = await _repository.GetByIdAsync(workflowId, ct);
        if (workflow is null) return Result<IReadOnlyList<WorkflowHistory>>.Failure(WorkflowErrors.NotFound);

        var history = await _repository.GetHistoryAsync(workflowId, ct);
        return Result<IReadOnlyList<WorkflowHistory>>.Success(history);
    }
}
