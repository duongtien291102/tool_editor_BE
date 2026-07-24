using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Domain.Entities.Orchestration;

public static class WorkflowStateMachine
{
    private static readonly Dictionary<WorkflowState, HashSet<WorkflowState>> AllowedTransitions = new()
    {
        { WorkflowState.Draft, new HashSet<WorkflowState> { WorkflowState.Queued, WorkflowState.Cancelled } },
        { WorkflowState.Queued, new HashSet<WorkflowState> { WorkflowState.Running, WorkflowState.Cancelled, WorkflowState.Failed } },
        { WorkflowState.Running, new HashSet<WorkflowState> { WorkflowState.Waiting, WorkflowState.PartiallyCompleted, WorkflowState.Completed, WorkflowState.Failed, WorkflowState.Cancelled } },
        { WorkflowState.Waiting, new HashSet<WorkflowState> { WorkflowState.Running, WorkflowState.Cancelled, WorkflowState.Failed } },
        { WorkflowState.PartiallyCompleted, new HashSet<WorkflowState> { WorkflowState.Running, WorkflowState.Completed, WorkflowState.Failed, WorkflowState.Cancelled } },
        { WorkflowState.Failed, new HashSet<WorkflowState> { WorkflowState.Queued, WorkflowState.Draft, WorkflowState.Cancelled } },
        { WorkflowState.Cancelled, new HashSet<WorkflowState> { WorkflowState.Queued, WorkflowState.Draft } },
        { WorkflowState.Completed, new HashSet<WorkflowState>() }
    };

    public static bool CanTransition(WorkflowState current, WorkflowState target)
    {
        return AllowedTransitions.TryGetValue(current, out var allowed) && allowed.Contains(target);
    }

    public static void ValidateTransition(WorkflowState current, WorkflowState target)
    {
        if (!CanTransition(current, target))
        {
            throw new InvalidOperationException($"Invalid workflow state transition from '{current}' to '{target}'.");
        }
    }
}
