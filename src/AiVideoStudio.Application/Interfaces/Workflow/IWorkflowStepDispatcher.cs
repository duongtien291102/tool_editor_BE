using AiVideoStudio.Domain.Entities;
namespace AiVideoStudio.Application.Interfaces.Workflow;
public interface IWorkflowStepDispatcher{Task<IReadOnlyDictionary<string,string>> ExecuteAsync(AIWorkflow workflow,WorkflowStep step,IReadOnlyDictionary<string,string> context,CancellationToken ct=default);}
