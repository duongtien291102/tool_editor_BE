using AiVideoStudio.Application.Features.Workflows;
using AiVideoStudio.Application.Features.Workflows.DTOs;
using AiVideoStudio.Application.Features.Workflows.Validators;
using AiVideoStudio.Application.Interfaces.Workflow;
using AiVideoStudio.Application.Interfaces.Render;
using AiVideoStudio.Application.Features.RenderJobs.DTOs;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Infrastructure.Workflow;
using AiVideoStudio.Infrastructure.Mongo.Repositories;
using AiVideoStudio.Shared.Responses;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using MongoDB.Driver;

namespace AiVideoStudio.UnitTests.Workflow;

public sealed class WorkflowEngineTests
{
    [Fact] public void Create_builds_ready_aggregate_with_variables(){var w=Create();w.Status.Should().Be(WorkflowStatus.Ready);w.Variables.Single().Value.Should().Be("landscape");w.Version.Should().Be(1);}
    [Fact] public void Create_rejects_cycle(){var a=Step("a",["b"]);var b=Step("b",["a"]);Action act=()=>AIWorkflow.Create("p","u","w",null,[a,b]);act.Should().Throw<InvalidOperationException>();}
    [Fact] public void Create_rejects_unknown_dependency(){Action act=()=>AIWorkflow.Create("p","u","w",null,[Step("a",["missing"])]);act.Should().Throw<InvalidOperationException>();}
    [Fact] public void Create_rejects_duplicate_step_ids(){Action act=()=>AIWorkflow.Create("p","u","w",null,[Step("a"),Step("a")]);act.Should().Throw<InvalidOperationException>();}
    [Fact] public void Running_workflow_can_pause_and_resume(){var w=Create();w.Start("e");w.Pause();w.IsPaused.Should().BeTrue();w.Resume();w.IsPaused.Should().BeFalse();}
    [Fact] public void Cancel_cascades_to_steps(){var w=Create();w.Cancel();w.Status.Should().Be(WorkflowStatus.Cancelled);w.Steps.Should().OnlyContain(x=>x.Status==WorkflowStepStatus.Cancelled);}
    [Fact] public void Failed_workflow_can_retry(){var w=Create();w.Start("e");var s=w.Steps.Single();s.Start();s.Fail("x");w.Fail("x");w.Retry();w.Status.Should().Be(WorkflowStatus.Ready);s.Status.Should().Be(WorkflowStepStatus.Pending);}
    [Fact] public void Step_retry_respects_limit(){var s=new WorkflowStep("s",WorkflowStepType.Custom,maxRetries:1);s.Start();s.Fail("x");s.Retry().Should().BeTrue();s.Start();s.Fail("x");s.Retry().Should().BeFalse();}
    [Fact] public void Variables_are_upserted(){var w=Create();w.UpdateVariables(new Dictionary<string,string>{{"style","portrait"},{"seed","7"}});w.Variables.Should().Contain(x=>x.Name=="style"&&x.Value=="portrait").And.Contain(x=>x.Name=="seed");}
    [Fact] public void Resolver_waits_for_dependencies(){var a=Step("a");var b=Step("b",["a"]);var w=AIWorkflow.Create("p","u","w",null,[a,b]);var resolver=new WorkflowResolver();resolver.ResolveReadySteps(w).Should().ContainSingle(x=>x.Id=="a");a.Start();a.Complete();resolver.ResolveReadySteps(w).Should().ContainSingle(x=>x.Id=="b");}
    [Theory][InlineData("style==cinematic",true)][InlineData("style!=cinematic",false)][InlineData("false",false)] public void Resolver_evaluates_conditions(string condition,bool expected){var resolver=new WorkflowResolver();var step=new WorkflowStep("s",WorkflowStepType.Custom,condition:condition);resolver.EvaluateCondition(step,new Dictionary<string,string>{{"style","cinematic"}}).Should().Be(expected);}
    [Fact] public async Task Scheduler_deduplicates_workflow_ids(){var s=new InMemoryWorkflowScheduler();await s.ScheduleAsync("w");await s.ScheduleAsync("w");(await s.DequeueAsync()).Should().Be("w");s.Remove("w").Should().BeFalse();}
    [Fact] public void Create_validator_rejects_empty_graph(){var v=new CreateWorkflowValidator();var r=v.Validate(new CreateWorkflowCommand("p","w",null,[],null));r.IsValid.Should().BeFalse();}
    [Fact] public void Step_validator_rejects_invalid_timeout(){var v=new StepDefinitionValidator();WorkflowStepDefinition definition=new(null,"x",WorkflowStepType.Custom,[],null,0,0,null);var r=v.Validate(definition);r.IsValid.Should().BeFalse();}
    [Fact] public async Task Executor_completes_DAG_and_propagates_output()
    {
        var workflow=AIWorkflow.Create("p","u","w",null,[Step("a"),Step("b",["a"])], [new("style","film")]);var repo=new MemoryWorkflowRepository(workflow);var executions=new MemoryExecutionRepository();var dispatcher=Substitute.For<IWorkflowStepDispatcher>();dispatcher.ExecuteAsync(Arg.Any<AIWorkflow>(),Arg.Any<WorkflowStep>(),Arg.Any<IReadOnlyDictionary<string,string>>(),Arg.Any<CancellationToken>()).Returns(new Dictionary<string,string>{{"assetId","asset-1"}});
        var executor=Executor(repo,executions,dispatcher);await executor.ExecuteAsync(workflow.Id);workflow.Status.Should().Be(WorkflowStatus.Completed);workflow.Steps.Should().OnlyContain(x=>x.Status==WorkflowStepStatus.Completed);executions.Item!.OutputContext.Should().ContainKey("assetId");
    }
    [Fact] public async Task Executor_times_out_and_fails_step()
    {
        var step=new WorkflowStep("slow",WorkflowStepType.Custom,timeoutSeconds:1,maxRetries:0,id:"slow");var workflow=AIWorkflow.Create("p","u","w",null,[step]);var repo=new MemoryWorkflowRepository(workflow);var executions=new MemoryExecutionRepository();var dispatcher=Substitute.For<IWorkflowStepDispatcher>();dispatcher.ExecuteAsync(Arg.Any<AIWorkflow>(),Arg.Any<WorkflowStep>(),Arg.Any<IReadOnlyDictionary<string,string>>(),Arg.Any<CancellationToken>()).Returns(call=>Slow(call.ArgAt<CancellationToken>(3)));
        await Executor(repo,executions,dispatcher).ExecuteAsync(workflow.Id);workflow.Status.Should().Be(WorkflowStatus.Failed);step.Status.Should().Be(WorkflowStepStatus.Failed);executions.Item!.Status.Should().Be(WorkflowStatus.Failed);
    }
    [Fact] public async Task Dispatcher_resolves_a_healthy_provider_by_capability_through_factory()
    {
        var provider=Substitute.For<IRenderProvider>();provider.Provider.Returns(RenderProvider.Internal);provider.ProviderName.Returns("registered-mock");provider.Capabilities.Returns(new HashSet<ProviderCapability>{ProviderCapability.GenerateImage});provider.RenderAsync(Arg.Any<RenderJob>(),Arg.Any<CancellationToken>()).Returns(RenderResult.Succeeded("{}",TimeSpan.Zero));
        var registry=Substitute.For<IRenderProviderRegistry>();registry.GetProviders().Returns([provider]);var factory=Substitute.For<IRenderProviderFactory>();factory.GetProvider(RenderProvider.Internal).Returns(provider);var health=Substitute.For<IProviderHealthChecker>();health.IsHealthy(RenderProvider.Internal).Returns(true);
        var workflow=AIWorkflow.Create("p","u","w",null,[new WorkflowStep("image",WorkflowStepType.GenerateImage,id:"image")]);var output=await new WorkflowStepDispatcher(registry,factory,health).ExecuteAsync(workflow,workflow.Steps.Single(),new Dictionary<string,string>());
        output["provider"].Should().Be("registered-mock");factory.Received(1).GetProvider(RenderProvider.Internal);
    }
    [Fact] public async Task Worker_dequeues_and_invokes_executor()
    {
        var scheduler=new InMemoryWorkflowScheduler();var executor=Substitute.For<IWorkflowExecutor>();var worker=new WorkflowWorker(scheduler,executor,NullLogger<WorkflowWorker>.Instance);await worker.StartAsync(CancellationToken.None);await scheduler.ScheduleAsync("w");
        for(var i=0;i<20&&!executor.ReceivedCalls().Any();i++)await Task.Delay(10);executor.ReceivedCalls().Should().ContainSingle();await worker.StopAsync(CancellationToken.None);
    }
    [Fact] public async Task Workflow_repository_inserts_through_Mongo_collection(){var collection=Substitute.For<IMongoCollection<AIWorkflow>>();var workflow=Create();await new AIWorkflowRepository(collection).AddAsync(workflow);await collection.Received().InsertOneAsync(workflow,Arg.Any<InsertOneOptions>(),Arg.Any<CancellationToken>());}
    [Fact] public async Task Execution_repository_inserts_through_Mongo_collection(){var collection=Substitute.For<IMongoCollection<WorkflowExecution>>();var execution=WorkflowExecution.Start("w","u");await new WorkflowExecutionRepository(collection).AddAsync(execution);await collection.Received().InsertOneAsync(execution,Arg.Any<InsertOneOptions>(),Arg.Any<CancellationToken>());}

    private static WorkflowExecutor Executor(IAIWorkflowRepository workflows,IWorkflowExecutionRepository executions,IWorkflowStepDispatcher dispatcher){var services=new ServiceCollection().AddSingleton(workflows).AddSingleton(executions).BuildServiceProvider();return new(services,new WorkflowResolver(),dispatcher,NullLogger<WorkflowExecutor>.Instance);}
    private static async Task<IReadOnlyDictionary<string,string>> Slow(CancellationToken ct){await Task.Delay(3000,ct);return new Dictionary<string,string>();}
    private static AIWorkflow Create()=>AIWorkflow.Create("p","u","workflow",null,[new WorkflowStep("step",WorkflowStepType.Custom,id:"step")],[new("style","landscape")]);
    private static WorkflowStep Step(string id,IEnumerable<string>? deps=null)=>new(id,WorkflowStepType.Custom,deps,id:id);
}

internal sealed class MemoryWorkflowRepository:IAIWorkflowRepository
{
    public AIWorkflow Item{get;}public MemoryWorkflowRepository(AIWorkflow item)=>Item=item;public Task<AIWorkflow?> GetByIdAsync(string id,CancellationToken ct=default)=>Task.FromResult<AIWorkflow?>(Item.Id==id?Item:null);public Task AddAsync(AIWorkflow w,CancellationToken ct=default)=>Task.CompletedTask;public Task UpdateAsync(AIWorkflow w,CancellationToken ct=default)=>Task.CompletedTask;public Task<PagedResult<AIWorkflow>> GetByProjectAsync(string id,int page,int size,CancellationToken ct=default)=>Task.FromResult(new PagedResult<AIWorkflow>([Item],1,page,size));
}
internal sealed class MemoryExecutionRepository:IWorkflowExecutionRepository
{
    public WorkflowExecution? Item{get;private set;}public Task<WorkflowExecution?> GetByIdAsync(string id,CancellationToken ct=default)=>Task.FromResult(Item?.Id==id?Item:null);public Task<WorkflowExecution?> GetLatestAsync(string id,CancellationToken ct=default)=>Task.FromResult(Item?.WorkflowId==id?Item:null);public Task AddAsync(WorkflowExecution e,CancellationToken ct=default){Item=e;return Task.CompletedTask;}public Task UpdateAsync(WorkflowExecution e,CancellationToken ct=default){Item=e;return Task.CompletedTask;}
}
