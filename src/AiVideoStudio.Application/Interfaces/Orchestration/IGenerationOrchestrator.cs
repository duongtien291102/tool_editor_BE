using AiVideoStudio.Domain.Entities.Orchestration;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.Responses;

namespace AiVideoStudio.Application.Interfaces.Orchestration;

public interface IGenerationOrchestrator
{
    Task<Result<GenerationWorkflow>> CreateWorkflowAsync(
        string projectId,
        string ownerId,
        string name,
        string? description,
        IEnumerable<OrchestrationStep> steps,
        WorkflowPolicy? policy = null,
        WorkflowExecutionContext? context = null,
        string? sceneId = null,
        string? shotId = null,
        CancellationToken ct = default);

    Task<Result<GenerationWorkflow>> QueueWorkflowAsync(string workflowId, CancellationToken ct = default);
    Task<Result<WorkflowResult>> ExecuteWorkflowAsync(string workflowId, CancellationToken ct = default);
    Task<Result<GenerationWorkflow>> RetryWorkflowAsync(string workflowId, string? stepId = null, CancellationToken ct = default);
    Task<Result<GenerationWorkflow>> ResumeWorkflowAsync(string workflowId, CancellationToken ct = default);
    Task<Result> CancelWorkflowAsync(string workflowId, string reason = "User requested cancellation.", CancellationToken ct = default);
    Task<Result<GenerationWorkflow>> GetWorkflowAsync(string workflowId, CancellationToken ct = default);
    Task<Result<WorkflowState>> GetWorkflowStatusAsync(string workflowId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<WorkflowHistory>>> GetWorkflowHistoryAsync(string workflowId, CancellationToken ct = default);
}
