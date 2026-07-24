using AiVideoStudio.Application.Interfaces.Orchestration;
using AiVideoStudio.Domain.Entities.Orchestration;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces.Orchestration;
using AiVideoStudio.Infrastructure.Orchestration;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace AiVideoStudio.UnitTests.Workflow;

public sealed class GenerationWorkflowWorkerTests
{
    [Fact]
    public async Task Worker_Dequeues_And_Executes_Queued_Workflows()
    {
        var step = new OrchestrationStep("Step 1", WorkflowStepType.GenerateImage, id: "step1");
        var workflow = GenerationWorkflow.Create("p1", "u1", "Queued Workflow", null, [step]);
        workflow.Queue();

        var repository = Substitute.For<IGenerationWorkflowRepository>();
        repository.GetQueuedWorkflowsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([workflow], []);

        var orchestrator = Substitute.For<IGenerationOrchestrator>();

        var services = new ServiceCollection();
        services.AddSingleton(repository);
        services.AddSingleton(orchestrator);
        var serviceProvider = services.BuildServiceProvider();

        var worker = new GenerationWorkflowWorker(serviceProvider, NullLogger<GenerationWorkflowWorker>.Instance);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(500);

        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        await orchestrator.Received().ExecuteWorkflowAsync(workflow.Id, Arg.Any<CancellationToken>());
    }
}
