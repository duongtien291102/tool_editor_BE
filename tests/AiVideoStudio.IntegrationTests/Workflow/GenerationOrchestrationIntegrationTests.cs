using AiVideoStudio.Application.Features.Orchestration.Services;
using AiVideoStudio.Application.Interfaces.Orchestration;
using AiVideoStudio.Domain.Entities.Orchestration;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces.Orchestration;
using AiVideoStudio.Shared.Logging;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AiVideoStudio.IntegrationTests.Workflow;

public sealed class GenerationOrchestrationIntegrationTests
{
    private readonly MemoryGenerationWorkflowRepository _repository = new();

    [Fact]
    public async Task End_To_End_Orchestration_Flow_Succeeds()
    {
        var step1 = new OrchestrationStep("Scene 1", WorkflowStepType.GenerateScript, id: "s1");
        var step2 = new OrchestrationStep("Shot 1", WorkflowStepType.GenerateImage, dependsOn: ["s1"], id: "s2");

        var orchestrator = new GenerationOrchestrator(
            _repository,
            new WorkflowSchedulerEngine(),
            Substitute.For<IOrchestrationDispatcher>(),
            Substitute.For<IAppLogger<GenerationOrchestrator>>());

        var createResult = await orchestrator.CreateWorkflowAsync("proj-1", "user-1", "End To End", null, [step1, step2]);
        createResult.IsSuccess.Should().BeTrue();
        var workflow = createResult.Value!;

        var queueResult = await orchestrator.QueueWorkflowAsync(workflow.Id);
        queueResult.IsSuccess.Should().BeTrue();

        var statusResult = await orchestrator.GetWorkflowStatusAsync(workflow.Id);
        statusResult.Value.Should().Be(WorkflowState.Queued);

        var historyResult = await orchestrator.GetWorkflowHistoryAsync(workflow.Id);
        historyResult.Value.Should().NotBeEmpty();
    }
}

internal sealed class MemoryGenerationWorkflowRepository : IGenerationWorkflowRepository
{
    private readonly Dictionary<string, GenerationWorkflow> _workflows = new();
    private readonly List<WorkflowHistory> _histories = new();

    public Task<GenerationWorkflow?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        _workflows.TryGetValue(id, out var w);
        return Task.FromResult(w);
    }

    public Task AddAsync(GenerationWorkflow workflow, CancellationToken ct = default)
    {
        _workflows[workflow.Id] = workflow;
        _histories.AddRange(workflow.History);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(GenerationWorkflow workflow, CancellationToken ct = default)
    {
        _workflows[workflow.Id] = workflow;
        foreach (var h in workflow.History)
        {
            if (!_histories.Any(x => x.Id == h.Id)) _histories.Add(h);
        }
        return Task.CompletedTask;
    }

    public Task<AiVideoStudio.Shared.Responses.PagedResult<GenerationWorkflow>> GetByProjectAsync(string projectId, int page, int size, CancellationToken ct = default)
    {
        var items = _workflows.Values.Where(x => x.ProjectId == projectId).ToList();
        return Task.FromResult(new AiVideoStudio.Shared.Responses.PagedResult<GenerationWorkflow>(items, items.Count, page, size));
    }

    public Task<IReadOnlyList<GenerationWorkflow>> GetQueuedWorkflowsAsync(int batchSize = 10, CancellationToken ct = default)
    {
        var items = _workflows.Values.Where(x => x.State == WorkflowState.Queued).Take(batchSize).ToList();
        return Task.FromResult<IReadOnlyList<GenerationWorkflow>>(items);
    }

    public Task<IReadOnlyList<WorkflowHistory>> GetHistoryAsync(string workflowId, CancellationToken ct = default)
    {
        var items = _histories.Where(x => x.WorkflowId == workflowId).ToList();
        return Task.FromResult<IReadOnlyList<WorkflowHistory>>(items);
    }

    public Task AddHistoryAsync(WorkflowHistory history, CancellationToken ct = default)
    {
        _histories.Add(history);
        return Task.CompletedTask;
    }
}
