using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces;

namespace AiVideoStudio.Infrastructure.Workflow;

public sealed class WorkflowResolver : IWorkflowResolver
{
    public IReadOnlyList<WorkflowStep> ResolveReadySteps(AIWorkflow workflow)
    {
        var successful=workflow.Steps.Where(x=>x.Status is WorkflowStepStatus.Completed or WorkflowStepStatus.Skipped).Select(x=>x.Id).ToHashSet();
        return workflow.Steps.Where(x=>x.Status is WorkflowStepStatus.Pending or WorkflowStepStatus.Waiting)
            .Where(x=>x.Dependencies.All(successful.Contains)).ToList();
    }

    public bool EvaluateCondition(WorkflowStep step,IReadOnlyDictionary<string,string> context)
    {
        if(string.IsNullOrWhiteSpace(step.Condition))return true;
        var expression=step.Condition.Trim();
        if(bool.TryParse(expression,out var literal))return literal;
        var op=expression.Contains("!=",StringComparison.Ordinal)?"!=":"==";
        var parts=expression.Split(op,StringSplitOptions.TrimEntries);
        if(parts.Length!=2)return false;
        var key=parts[0].TrimStart('$','{').TrimEnd('}');
        var expected=parts[1].Trim('"','\'');
        var equal=context.TryGetValue(key,out var actual)&&string.Equals(actual,expected,StringComparison.OrdinalIgnoreCase);
        return op=="=="?equal:!equal;
    }
}
