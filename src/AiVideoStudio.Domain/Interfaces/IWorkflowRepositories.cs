using AiVideoStudio.Domain.Entities;using AiVideoStudio.Shared.Responses;
namespace AiVideoStudio.Domain.Interfaces;
public interface IAIWorkflowRepository{Task<AIWorkflow?> GetByIdAsync(string id,CancellationToken ct=default);Task AddAsync(AIWorkflow workflow,CancellationToken ct=default);Task UpdateAsync(AIWorkflow workflow,CancellationToken ct=default);Task<PagedResult<AIWorkflow>> GetByProjectAsync(string projectId,int page,int size,CancellationToken ct=default);}
public interface IWorkflowExecutionRepository{Task<WorkflowExecution?> GetByIdAsync(string id,CancellationToken ct=default);Task<WorkflowExecution?> GetLatestAsync(string workflowId,CancellationToken ct=default);Task AddAsync(WorkflowExecution execution,CancellationToken ct=default);Task UpdateAsync(WorkflowExecution execution,CancellationToken ct=default);}
public interface IWorkflowExecutor{Task ExecuteAsync(string workflowId,CancellationToken ct=default);void Cancel(string workflowId);}
public interface IWorkflowScheduler{ValueTask ScheduleAsync(string workflowId,CancellationToken ct=default);ValueTask<string> DequeueAsync(CancellationToken ct=default);bool Remove(string workflowId);}
public interface IWorkflowResolver{IReadOnlyList<WorkflowStep> ResolveReadySteps(AIWorkflow workflow);bool EvaluateCondition(WorkflowStep step,IReadOnlyDictionary<string,string> context);}
