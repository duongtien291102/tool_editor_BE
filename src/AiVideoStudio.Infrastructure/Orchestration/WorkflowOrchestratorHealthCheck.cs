using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces.Orchestration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AiVideoStudio.Infrastructure.Orchestration;

public sealed class WorkflowOrchestratorHealthCheck : IHealthCheck
{
    private readonly IGenerationWorkflowRepository _repository;

    public WorkflowOrchestratorHealthCheck(IGenerationWorkflowRepository repository)
    {
        _repository = repository;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var queued = await _repository.GetQueuedWorkflowsAsync(100, cancellationToken);
            int queuedCount = queued.Count;

            var data = new Dictionary<string, object>
            {
                { "QueuedWorkflows", queuedCount },
                { "EngineStatus", "Healthy" },
                { "Timestamp", DateTimeOffset.UtcNow }
            };

            if (queuedCount > 1000)
            {
                return HealthCheckResult.Degraded("Workflow queue depth high.", data: data);
            }

            return HealthCheckResult.Healthy("AI Generation Orchestration Engine is operational.", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Workflow Orchestrator Health Check failed.", ex);
        }
    }
}
