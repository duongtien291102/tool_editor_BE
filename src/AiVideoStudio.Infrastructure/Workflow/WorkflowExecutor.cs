using System.Collections.Concurrent;
using AiVideoStudio.Application.Interfaces.Workflow;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AiVideoStudio.Infrastructure.Workflow;

public sealed class WorkflowExecutor:IWorkflowExecutor
{
    private readonly IServiceProvider _services;private readonly IWorkflowResolver _resolver;private readonly IWorkflowStepDispatcher _dispatcher;private readonly ILogger<WorkflowExecutor> _logger;
    private readonly ConcurrentDictionary<string,CancellationTokenSource> _active=new();
    public WorkflowExecutor(IServiceProvider services,IWorkflowResolver resolver,IWorkflowStepDispatcher dispatcher,ILogger<WorkflowExecutor> logger){_services=services;_resolver=resolver;_dispatcher=dispatcher;_logger=logger;}
    public void Cancel(string id){if(_active.TryGetValue(id,out var source))source.Cancel();}
    public async Task ExecuteAsync(string id,CancellationToken ct=default)
    {
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(ct);if(!_active.TryAdd(id,linked))return;
        try
        {
            using var scope=_services.CreateScope();var workflows=scope.ServiceProvider.GetRequiredService<IAIWorkflowRepository>();var executions=scope.ServiceProvider.GetRequiredService<IWorkflowExecutionRepository>();
            var workflow=await workflows.GetByIdAsync(id,linked.Token);if(workflow is null||workflow.Status!=WorkflowStatus.Ready)return;
            var execution=WorkflowExecution.Start(id,workflow.OwnerId);await executions.AddAsync(execution,linked.Token);workflow.Start(execution.Id);await workflows.UpdateAsync(workflow,linked.Token);
            var context=workflow.Variables.ToDictionary(x=>x.Name,x=>x.Value);
            while(true)
            {
                linked.Token.ThrowIfCancellationRequested();workflow=await workflows.GetByIdAsync(id,linked.Token)??throw new InvalidOperationException("Workflow disappeared.");
                if(workflow.Status==WorkflowStatus.Cancelled)throw new OperationCanceledException(linked.Token);
                if(workflow.IsPaused){await Task.Delay(50,linked.Token);continue;}
                var ready=_resolver.ResolveReadySteps(workflow);
                if(ready.Count==0)
                {
                    if(workflow.Steps.All(x=>x.Status is WorkflowStepStatus.Completed or WorkflowStepStatus.Skipped)){workflow.Complete();execution.Complete(context);await workflows.UpdateAsync(workflow,linked.Token);await executions.UpdateAsync(execution,linked.Token);return;}
                    throw new InvalidOperationException("Workflow graph cannot make progress.");
                }
                foreach(var step in ready)
                {
                    if(!_resolver.EvaluateCondition(step,context)){step.Skip();await workflows.UpdateAsync(workflow,linked.Token);continue;}
                    step.Start();workflow.StepStarted(step.Id);await workflows.UpdateAsync(workflow,linked.Token);
                    try
                    {
                        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(linked.Token);timeout.CancelAfter(TimeSpan.FromSeconds(step.TimeoutSeconds));
                        var output=await _dispatcher.ExecuteAsync(workflow,step,context,timeout.Token);step.Complete(output.ToDictionary());foreach(var pair in output){context[$"{step.Id}.{pair.Key}"]=pair.Value;context[pair.Key]=pair.Value;}workflow.StepCompleted(step.Id);await workflows.UpdateAsync(workflow,linked.Token);
                    }
                    catch(OperationCanceledException) when(!linked.IsCancellationRequested){if(!await FailOrRetry(step,"Step timed out.",workflow,workflows,executions,execution,linked.Token))return;}
                    catch(Exception ex){if(!await FailOrRetry(step,ex.Message,workflow,workflows,executions,execution,linked.Token))return;}
                }
            }
        }
        catch(OperationCanceledException)
        {
            using var scope=_services.CreateScope();var workflows=scope.ServiceProvider.GetRequiredService<IAIWorkflowRepository>();var executions=scope.ServiceProvider.GetRequiredService<IWorkflowExecutionRepository>();var workflow=await workflows.GetByIdAsync(id,CancellationToken.None);if(workflow is not null&&workflow.Status!=WorkflowStatus.Cancelled){try{workflow.Cancel();await workflows.UpdateAsync(workflow,CancellationToken.None);}catch(InvalidOperationException){}}var execution=await executions.GetLatestAsync(id,CancellationToken.None);if(execution is not null&&execution.Status==WorkflowStatus.Running){execution.Cancel();await executions.UpdateAsync(execution,CancellationToken.None);}
        }
        catch(Exception ex){_logger.LogError(ex,"Workflow {WorkflowId} failed unexpectedly.",id);}
        finally{_active.TryRemove(id,out _);}
    }
    private static async Task<bool> FailOrRetry(WorkflowStep step,string error,AIWorkflow workflow,IAIWorkflowRepository workflows,IWorkflowExecutionRepository executions,WorkflowExecution execution,CancellationToken ct)
    {step.Fail(error);workflow.StepFailed(step.Id,error);if(step.Retry()){await workflows.UpdateAsync(workflow,ct);return true;}workflow.Fail(error);execution.Fail(error);await workflows.UpdateAsync(workflow,ct);await executions.UpdateAsync(execution,ct);return false;}
}
