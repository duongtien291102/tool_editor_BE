using AiVideoStudio.Domain.Entities.Orchestration;
using AiVideoStudio.Shared.Responses;

namespace AiVideoStudio.Domain.Interfaces.Orchestration;

public interface IGenerationWorkflowRepository
{
    Task<GenerationWorkflow?> GetByIdAsync(string id, CancellationToken ct = default);
    Task AddAsync(GenerationWorkflow workflow, CancellationToken ct = default);
    Task UpdateAsync(GenerationWorkflow workflow, CancellationToken ct = default);
    Task<PagedResult<GenerationWorkflow>> GetByProjectAsync(string projectId, int page, int size, CancellationToken ct = default);
    Task<IReadOnlyList<GenerationWorkflow>> GetQueuedWorkflowsAsync(int batchSize = 10, CancellationToken ct = default);
    Task<IReadOnlyList<WorkflowHistory>> GetHistoryAsync(string workflowId, CancellationToken ct = default);
    Task AddHistoryAsync(WorkflowHistory history, CancellationToken ct = default);
}
