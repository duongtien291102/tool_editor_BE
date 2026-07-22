using System.Text.Json;
using AiVideoStudio.Application.Interfaces.Render;
using AiVideoStudio.Application.Interfaces.Workflow;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Infrastructure.Workflow;

public sealed class WorkflowStepDispatcher:IWorkflowStepDispatcher
{
    private readonly IRenderProviderRegistry _registry;private readonly IRenderProviderFactory _factory;private readonly IProviderHealthChecker _health;
    public WorkflowStepDispatcher(IRenderProviderRegistry registry,IRenderProviderFactory factory,IProviderHealthChecker health){_registry=registry;_factory=factory;_health=health;}
    public async Task<IReadOnlyDictionary<string,string>> ExecuteAsync(AIWorkflow workflow,WorkflowStep step,IReadOnlyDictionary<string,string> context,CancellationToken ct=default)
    {
        var capability=Capability(step.Type);
        if(capability is null)return new Dictionary<string,string>{{"operation",step.Type.ToString()},{"status","Completed"}};
        var descriptor=_registry.GetProviders().FirstOrDefault(x=>x.Capabilities.Contains(capability.Value)&&_health.IsHealthy(x.Provider))
            ??throw new InvalidOperationException($"No healthy provider supports capability '{capability}'.");
        var provider=_factory.GetProvider(descriptor.Provider);
        using var payload=JsonDocument.Parse(JsonSerializer.Serialize(new{step.InputContext,Context=context}));
        var job=RenderJob.Create(workflow.ProjectId,workflow.OwnerId,JobType(step.Type),provider.Provider,inputPayload:payload);
        var result=await provider.RenderAsync(job,ct);
        if(!result.IsSuccess)throw new InvalidOperationException(result.ErrorMessage??result.ErrorCode??"Provider failed.");
        return new Dictionary<string,string>{{"provider",provider.ProviderName},{"payload",result.OutputPayload??string.Empty},{"durationMs",result.Duration.TotalMilliseconds.ToString("0")}};
    }
    private static ProviderCapability? Capability(WorkflowStepType type)=>type switch{WorkflowStepType.GenerateImage=>ProviderCapability.GenerateImage,WorkflowStepType.GenerateVideo=>ProviderCapability.GenerateVideo,WorkflowStepType.GenerateVoice=>ProviderCapability.GenerateVoice,WorkflowStepType.GenerateSubtitle=>ProviderCapability.GenerateSubtitle,WorkflowStepType.Upscale=>ProviderCapability.Upscale,_=>null};
    private static RenderJobType JobType(WorkflowStepType type)=>type switch{WorkflowStepType.GenerateImage=>RenderJobType.GenerateImage,WorkflowStepType.GenerateVideo=>RenderJobType.GenerateVideo,WorkflowStepType.GenerateVoice=>RenderJobType.GenerateVoice,WorkflowStepType.GenerateSubtitle=>RenderJobType.GenerateSubtitle,_=>RenderJobType.RenderTimeline};
}
