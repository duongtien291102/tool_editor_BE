using AiVideoStudio.Domain.Entities.Orchestration;
using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Application.Features.Orchestration.Services;

public sealed class StepBatch
{
    public string BatchId { get; } = Guid.NewGuid().ToString();
    public string? Provider { get; }
    public string? Resolution { get; }
    public string? Style { get; }
    public string? AspectRatio { get; }
    public string? Model { get; }
    public IReadOnlyList<OrchestrationStep> Steps { get; }

    public StepBatch(
        string? provider,
        string? resolution,
        string? style,
        string? aspectRatio,
        string? model,
        IEnumerable<OrchestrationStep> steps)
    {
        Provider = provider;
        Resolution = resolution;
        Style = style;
        AspectRatio = aspectRatio;
        Model = model;
        Steps = steps.ToList().AsReadOnly();
    }
}

public interface IWorkflowSchedulerEngine
{
    IReadOnlyList<OrchestrationStep> GetReadySteps(GenerationWorkflow workflow);
    IReadOnlyList<List<OrchestrationStep>> GetParallelStepGroups(GenerationWorkflow workflow);
    IReadOnlyList<StepBatch> ScheduleBatches(IEnumerable<OrchestrationStep> steps, int maxBatchSize);
}

public sealed class WorkflowSchedulerEngine : IWorkflowSchedulerEngine
{
    public IReadOnlyList<OrchestrationStep> GetReadySteps(GenerationWorkflow workflow)
    {
        var completedOrSkipped = workflow.Steps
            .Where(s => s.Status is WorkflowStepStatus.Completed or WorkflowStepStatus.Skipped)
            .Select(s => s.Id)
            .ToHashSet();

        return workflow.Steps
            .Where(s => s.Status == WorkflowStepStatus.Pending)
            .Where(s => s.DependsOn.All(dep => completedOrSkipped.Contains(dep)))
            .ToList();
    }

    public IReadOnlyList<List<OrchestrationStep>> GetParallelStepGroups(GenerationWorkflow workflow)
    {
        var ready = GetReadySteps(workflow);
        if (ready.Count == 0) return Array.Empty<List<OrchestrationStep>>();

        // Group by explicit ParallelGroupId if present, or group by dependee step set
        var groups = ready
            .GroupBy(s => !string.IsNullOrEmpty(s.ParallelGroupId)
                ? s.ParallelGroupId
                : string.Join(",", s.DependsOn.OrderBy(x => x)))
            .Select(g => g.ToList())
            .ToList();

        return groups;
    }

    public IReadOnlyList<StepBatch> ScheduleBatches(IEnumerable<OrchestrationStep> steps, int maxBatchSize)
    {
        if (maxBatchSize <= 0) maxBatchSize = 1;

        var batches = new List<StepBatch>();

        // Group shots by Provider, Resolution, Style, AspectRatio, Model
        var groupedShots = steps
            .GroupBy(s => new
            {
                Provider = s.Provider ?? string.Empty,
                Resolution = s.Resolution ?? string.Empty,
                Style = s.Style ?? string.Empty,
                AspectRatio = s.AspectRatio ?? string.Empty,
                Model = s.Model ?? string.Empty
            });

        foreach (var group in groupedShots)
        {
            var shotList = group.ToList();
            for (int i = 0; i < shotList.Count; i += maxBatchSize)
            {
                var chunk = shotList.Skip(i).Take(maxBatchSize);
                batches.Add(new StepBatch(
                    group.Key.Provider,
                    group.Key.Resolution,
                    group.Key.Style,
                    group.Key.AspectRatio,
                    group.Key.Model,
                    chunk));
            }
        }

        return batches;
    }
}
