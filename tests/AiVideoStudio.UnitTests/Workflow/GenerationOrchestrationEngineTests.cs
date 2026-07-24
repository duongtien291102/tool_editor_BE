using AiVideoStudio.Application.Features.Orchestration.Services;
using AiVideoStudio.Domain.Entities.Orchestration;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Events.Workflows;
using AiVideoStudio.Domain.Interfaces.Orchestration;
using AiVideoStudio.Shared.Logging;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AiVideoStudio.UnitTests.Workflow;

public sealed class GenerationOrchestrationEngineTests
{
    private readonly IAppLogger<GenerationOrchestrator> _logger = Substitute.For<IAppLogger<GenerationOrchestrator>>();

    [Fact]
    public void Workflow_Creation_Builds_Draft_Aggregate_With_Valid_DAG()
    {
        var step1 = new OrchestrationStep("Scene 1", WorkflowStepType.GenerateScript, id: "scene1");
        var step2 = new OrchestrationStep("Shot A", WorkflowStepType.GenerateImage, dependsOn: ["scene1"], id: "shotA");

        var workflow = GenerationWorkflow.Create("p1", "u1", "AI Generation Workflow", "Test Description", [step1, step2]);

        workflow.State.Should().Be(WorkflowState.Draft);
        workflow.ProjectId.Should().Be("p1");
        workflow.Steps.Should().HaveCount(2);
        workflow.History.Should().ContainSingle(h => h.State == WorkflowState.Draft);
        workflow.DomainEvents.Should().ContainSingle(e => e is OrchestrationWorkflowCreatedEvent);
    }

    [Fact]
    public void Workflow_Creation_Rejects_Circular_Dependency()
    {
        var stepA = new OrchestrationStep("Step A", WorkflowStepType.GenerateImage, dependsOn: ["stepB"], id: "stepA");
        var stepB = new OrchestrationStep("Step B", WorkflowStepType.GenerateImage, dependsOn: ["stepA"], id: "stepB");

        Action act = () => GenerationWorkflow.Create("p1", "u1", "Cyclic Workflow", null, [stepA, stepB]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*circular dependency*");
    }

    [Fact]
    public void Workflow_Creation_Rejects_Unknown_Dependency()
    {
        var step = new OrchestrationStep("Step A", WorkflowStepType.GenerateImage, dependsOn: ["missingStep"], id: "stepA");

        Action act = () => GenerationWorkflow.Create("p1", "u1", "Invalid Dep Workflow", null, [step]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*unknown dependency*");
    }

    [Fact]
    public void StateMachine_Prevents_Invalid_State_Transition()
    {
        Action act = () => WorkflowStateMachine.ValidateTransition(WorkflowState.Draft, WorkflowState.Completed);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid workflow state transition*");
    }

    [Fact]
    public void Scheduler_Resolves_Parallel_Step_Groups()
    {
        var scene = new OrchestrationStep("Scene 1", WorkflowStepType.GenerateScript, id: "scene1");
        var shotA = new OrchestrationStep("Shot A", WorkflowStepType.GenerateImage, dependsOn: ["scene1"], id: "shotA");
        var shotB = new OrchestrationStep("Shot B", WorkflowStepType.GenerateImage, dependsOn: ["scene1"], id: "shotB");
        var shotC = new OrchestrationStep("Shot C", WorkflowStepType.GenerateImage, dependsOn: ["scene1"], id: "shotC");

        var workflow = GenerationWorkflow.Create("p1", "u1", "Parallel Workflow", null, [scene, shotA, shotB, shotC]);
        var scheduler = new WorkflowSchedulerEngine();

        var ready1 = scheduler.GetReadySteps(workflow);
        ready1.Should().ContainSingle(s => s.Id == "scene1");

        scene.Start();
        scene.Complete();

        var ready2 = scheduler.GetReadySteps(workflow);
        ready2.Should().HaveCount(3);
        ready2.Select(s => s.Id).Should().BeEquivalentTo(["shotA", "shotB", "shotC"]);

        var parallelGroups = scheduler.GetParallelStepGroups(workflow);
        parallelGroups.Should().HaveCount(1);
        parallelGroups[0].Should().HaveCount(3);
    }

    [Fact]
    public void Scheduler_Batches_Shots_With_Matching_Attributes()
    {
        var steps = new List<OrchestrationStep>
        {
            new("Shot 1", WorkflowStepType.GenerateImage, provider: "Runway", resolution: "1080p", style: "Cinematic", aspectRatio: "16:9", model: "Gen-2"),
            new("Shot 2", WorkflowStepType.GenerateImage, provider: "Runway", resolution: "1080p", style: "Cinematic", aspectRatio: "16:9", model: "Gen-2"),
            new("Shot 3", WorkflowStepType.GenerateImage, provider: "OpenAI", resolution: "1080p", style: "Anime", aspectRatio: "16:9", model: "DALL-E-3")
        };

        var scheduler = new WorkflowSchedulerEngine();
        var batches = scheduler.ScheduleBatches(steps, maxBatchSize: 5);

        batches.Should().HaveCount(2);
        batches.First(b => b.Provider == "Runway").Steps.Should().HaveCount(2);
        batches.First(b => b.Provider == "OpenAI").Steps.Should().HaveCount(1);
    }

    [Fact]
    public async Task Orchestrator_Executes_Workflow_Successfully_And_Publishes_Events()
    {
        var step = new OrchestrationStep("Shot 1", WorkflowStepType.GenerateImage, id: "step1");
        var workflow = GenerationWorkflow.Create("p1", "u1", "Workflow", null, [step]);
        workflow.Queue();

        var repo = Substitute.For<IGenerationWorkflowRepository>();
        repo.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);

        var scheduler = new WorkflowSchedulerEngine();
        var dispatcher = Substitute.For<IOrchestrationDispatcher>();
        dispatcher.DispatchStepAsync(Arg.Any<GenerationWorkflow>(), Arg.Any<OrchestrationStep>(), Arg.Any<WorkflowExecutionContext>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { { "output", "success" } });

        var orchestrator = new GenerationOrchestrator(repo, scheduler, dispatcher, _logger);

        var result = await orchestrator.ExecuteWorkflowAsync(workflow.Id);

        result.IsSuccess.Should().BeTrue();
        workflow.State.Should().Be(WorkflowState.Completed);
        step.Status.Should().Be(WorkflowStepStatus.Completed);
        workflow.DomainEvents.Should().Contain(e => e is OrchestrationWorkflowCompletedEvent);
    }

    [Fact]
    public async Task Orchestrator_Retries_Failed_Step_And_Compensates_If_Max_Retries_Exceeded()
    {
        var step = new OrchestrationStep("Shot 1", WorkflowStepType.GenerateImage, maxRetries: 1, id: "step1");
        var workflow = GenerationWorkflow.Create("p1", "u1", "Failing Workflow", null, [step]);
        workflow.Queue();

        var repo = Substitute.For<IGenerationWorkflowRepository>();
        repo.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);

        var scheduler = new WorkflowSchedulerEngine();
        var dispatcher = Substitute.For<IOrchestrationDispatcher>();
        dispatcher.DispatchStepAsync(Arg.Any<GenerationWorkflow>(), Arg.Any<OrchestrationStep>(), Arg.Any<WorkflowExecutionContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("API failure"));

        var orchestrator = new GenerationOrchestrator(repo, scheduler, dispatcher, _logger);

        var result = await orchestrator.ExecuteWorkflowAsync(workflow.Id);

        result.IsSuccess.Should().BeFalse();
        workflow.State.Should().Be(WorkflowState.Failed);
        await dispatcher.Received(1).ExecuteCompensationAsync(Arg.Any<GenerationWorkflow>(), Arg.Any<OrchestrationStep>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Orchestrator_Uses_Fallback_Provider_When_Configured()
    {
        var policy = new WorkflowPolicy(maxRetry: 0, providerFallback: "FallbackProvider");
        var step = new OrchestrationStep("Shot 1", WorkflowStepType.GenerateImage, provider: "PrimaryProvider", maxRetries: 0, id: "step1");
        var workflow = GenerationWorkflow.Create("p1", "u1", "Fallback Workflow", null, [step], policy: policy);
        workflow.Queue();

        var repo = Substitute.For<IGenerationWorkflowRepository>();
        repo.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);

        var scheduler = new WorkflowSchedulerEngine();
        var dispatcher = Substitute.For<IOrchestrationDispatcher>();
        int callCount = 0;
        dispatcher.DispatchStepAsync(Arg.Any<GenerationWorkflow>(), Arg.Any<OrchestrationStep>(), Arg.Any<WorkflowExecutionContext>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                callCount++;
                if (callCount == 1) throw new InvalidOperationException("Primary provider offline");
                return new Dictionary<string, string> { { "status", "ok" } };
            });

        var orchestrator = new GenerationOrchestrator(repo, scheduler, dispatcher, _logger);

        var result = await orchestrator.ExecuteWorkflowAsync(workflow.Id);

        result.IsSuccess.Should().BeTrue();
        step.Provider.Should().Be("FallbackProvider");
    }

    [Fact]
    public async Task Orchestrator_Cancels_Workflow_On_Request()
    {
        var step = new OrchestrationStep("Shot 1", WorkflowStepType.GenerateImage, id: "step1");
        var workflow = GenerationWorkflow.Create("p1", "u1", "Workflow to Cancel", null, [step]);
        workflow.Queue();

        var repo = Substitute.For<IGenerationWorkflowRepository>();
        repo.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);

        var orchestrator = new GenerationOrchestrator(repo, new WorkflowSchedulerEngine(), Substitute.For<IOrchestrationDispatcher>(), _logger);

        var cancelResult = await orchestrator.CancelWorkflowAsync(workflow.Id, "Testing cancellation");

        cancelResult.IsSuccess.Should().BeTrue();
        workflow.State.Should().Be(WorkflowState.Cancelled);
        step.Status.Should().Be(WorkflowStepStatus.Cancelled);
    }
}
