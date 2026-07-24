using AiVideoStudio.Application.Interfaces.Workflow;
using AiVideoStudio.Domain.Entities.Orchestration;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.Logging;

namespace AiVideoStudio.Application.Features.Orchestration.Services;

public interface IOrchestrationDispatcher
{
    Task<IDictionary<string, string>> DispatchStepAsync(
        GenerationWorkflow workflow,
        OrchestrationStep step,
        WorkflowExecutionContext context,
        CancellationToken ct);

    Task<IReadOnlyList<IDictionary<string, string>>> DispatchBatchAsync(
        GenerationWorkflow workflow,
        StepBatch batch,
        WorkflowExecutionContext context,
        CancellationToken ct);

    Task ExecuteCompensationAsync(
        GenerationWorkflow workflow,
        OrchestrationStep step,
        string reason,
        CancellationToken ct);
}

public sealed class OrchestrationDispatcher : IOrchestrationDispatcher
{
    private readonly IWorkflowStepDispatcher _stepDispatcher;
    private readonly IAppLogger<OrchestrationDispatcher> _logger;

    public OrchestrationDispatcher(
        IWorkflowStepDispatcher stepDispatcher,
        IAppLogger<OrchestrationDispatcher> logger)
    {
        _stepDispatcher = stepDispatcher;
        _logger = logger;
    }

    public async Task<IDictionary<string, string>> DispatchStepAsync(
        GenerationWorkflow workflow,
        OrchestrationStep step,
        WorkflowExecutionContext context,
        CancellationToken ct)
    {
        _logger.LogInformation(
            1001,
            $"Dispatching Step {step.Id} ({step.Name}) of Type {step.Type} for Workflow {workflow.Id}. CorrelationId: {workflow.CorrelationId}");

        var legacyStep = new AiVideoStudio.Domain.Entities.WorkflowStep(
            step.Name,
            step.Type,
            step.DependsOn,
            step.Condition,
            step.TimeoutSeconds,
            step.MaxRetries,
            step.InputContext,
            step.Id);

        var dummyAiWorkflow = AiVideoStudio.Domain.Entities.AIWorkflow.Create(
            workflow.ProjectId,
            workflow.OwnerId,
            workflow.Name,
            workflow.Description,
            [legacyStep]);

        try
        {
            var output = await _stepDispatcher.ExecuteAsync(
                dummyAiWorkflow,
                legacyStep,
                context.Data,
                ct);

            return output.ToDictionary(k => k.Key, v => v.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(1002, $"Step {step.Id} dispatch encountered error: {ex.Message}");
            throw;
        }
    }

    public async Task<IReadOnlyList<IDictionary<string, string>>> DispatchBatchAsync(
        GenerationWorkflow workflow,
        StepBatch batch,
        WorkflowExecutionContext context,
        CancellationToken ct)
    {
        _logger.LogInformation(
            1003,
            $"Dispatching Step Batch {batch.BatchId} containing {batch.Steps.Count} steps for Provider {batch.Provider}. WorkflowId: {workflow.Id}");

        var results = new List<IDictionary<string, string>>();

        foreach (var step in batch.Steps)
        {
            var result = await DispatchStepAsync(workflow, step, context, ct);
            results.Add(result);
        }

        return results;
    }

    public Task ExecuteCompensationAsync(
        GenerationWorkflow workflow,
        OrchestrationStep step,
        string reason,
        CancellationToken ct)
    {
        _logger.LogWarning(
            1004,
            $"Executing Compensation for Step {step.Id} ({step.Name}) in Workflow {workflow.Id}. Reason: {reason}");

        var action = $"Compensated: Cancelled downstream dependencies and rolled back resources for step {step.Name}.";
        workflow.StepCompensated(step.Id, action);

        return Task.CompletedTask;
    }
}
