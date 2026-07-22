using AiVideoStudio.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AiVideoStudio.Infrastructure.Workflow;

public sealed class WorkflowWorker:BackgroundService
{
    private readonly IWorkflowScheduler _scheduler;private readonly IWorkflowExecutor _executor;private readonly ILogger<WorkflowWorker> _logger;
    public WorkflowWorker(IWorkflowScheduler scheduler,IWorkflowExecutor executor,ILogger<WorkflowWorker> logger){_scheduler=scheduler;_executor=executor;_logger=logger;}
    protected override async Task ExecuteAsync(CancellationToken ct){while(!ct.IsCancellationRequested){try{var id=await _scheduler.DequeueAsync(ct);_=Task.Run(()=>_executor.ExecuteAsync(id,ct),CancellationToken.None);}catch(OperationCanceledException)when(ct.IsCancellationRequested){}catch(Exception ex){_logger.LogError(ex,"Workflow worker loop failed.");}}}
}
