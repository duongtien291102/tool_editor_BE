using AiVideoStudio.Application.Interfaces.Orchestration;
using AiVideoStudio.Domain.Interfaces.Orchestration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AiVideoStudio.Infrastructure.Orchestration;

public sealed class GenerationWorkflowWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GenerationWorkflowWorker> _logger;

    public GenerationWorkflowWorker(
        IServiceProvider serviceProvider,
        ILogger<GenerationWorkflowWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GenerationWorkflowWorker started. Polling queue for ready workflows...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IGenerationWorkflowRepository>();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IGenerationOrchestrator>();

                var queuedWorkflows = await repository.GetQueuedWorkflowsAsync(batchSize: 5, ct: stoppingToken);

                if (queuedWorkflows.Count == 0)
                {
                    await Task.Delay(100, stoppingToken);
                    continue;
                }

                foreach (var workflow in queuedWorkflows)
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    _logger.LogInformation("Worker dequeued Workflow {WorkflowId}. Triggering execution...", workflow.Id);
                    _ = Task.Run(() => orchestrator.ExecuteWorkflowAsync(workflow.Id, stoppingToken), CancellationToken.None);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("GenerationWorkflowWorker stopping gracefully due to cancellation request.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerationWorkflowWorker background polling loop.");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
