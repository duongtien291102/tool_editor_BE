using System.Net;
using System.Net.Http.Json;
using AiVideoStudio.Api.Controllers.v1;
using AiVideoStudio.Application.Features.Workflows.DTOs;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.Responses;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AiVideoStudio.IntegrationTests.Controllers;

public sealed class WorkflowControllerIntegrationTests:IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;private readonly HttpClient _client;
    public WorkflowControllerIntegrationTests(CustomWebApplicationFactory factory){_factory=factory;_client=factory.CreateClient();factory.CurrentUser.IsAuthenticated.Returns(true);factory.CurrentUser.UserId.Returns("owner");factory.CurrentUser.Roles.Returns(Array.Empty<string>());factory.ProjectRepository.GetByIdAsync("p",Arg.Any<CancellationToken>()).Returns(Project.Create("project","owner"));}

    [Fact] public async Task Create_returns_created_and_persists(){AIWorkflow? saved=null;_factory.AIWorkflowRepository.When(x=>x.AddAsync(Arg.Any<AIWorkflow>(),Arg.Any<CancellationToken>())).Do(x=>saved=x.Arg<AIWorkflow>());var response=await _client.PostAsJsonAsync("/api/v1/workflows",Request());response.StatusCode.Should().Be(HttpStatusCode.Created);saved.Should().NotBeNull();saved!.Status.Should().Be(WorkflowStatus.Ready);}
    [Fact] public async Task Create_rejects_cyclic_graph(){var request=Request([Def("a",["b"]),Def("b",["a"])]);(await _client.PostAsJsonAsync("/api/v1/workflows",request)).StatusCode.Should().Be(HttpStatusCode.BadRequest);}
    [Fact] public async Task Get_and_execution_return_owner_data(){var w=Workflow();var e=WorkflowExecution.Start(w.Id,"owner");_factory.AIWorkflowRepository.GetByIdAsync(w.Id,Arg.Any<CancellationToken>()).Returns(w);_factory.WorkflowExecutionRepository.GetLatestAsync(w.Id,Arg.Any<CancellationToken>()).Returns(e);(await _client.GetAsync($"/api/v1/workflows/{w.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);(await _client.GetAsync($"/api/v1/workflows/{w.Id}/execution")).StatusCode.Should().Be(HttpStatusCode.OK);}
    [Fact] public async Task List_returns_project_page(){var w=Workflow();_factory.AIWorkflowRepository.GetByProjectAsync("p",1,20,Arg.Any<CancellationToken>()).Returns(new PagedResult<AIWorkflow>([w],1,1,20));(await _client.GetAsync("/api/v1/projects/p/workflows")).StatusCode.Should().Be(HttpStatusCode.OK);}
    [Fact] public async Task Run_returns_accepted_and_schedules_background_execution(){var w=Workflow();_factory.AIWorkflowRepository.GetByIdAsync(w.Id,Arg.Any<CancellationToken>()).Returns(w);(await _client.PostAsync($"/api/v1/workflows/{w.Id}/run",null)).StatusCode.Should().Be(HttpStatusCode.Accepted);}
    [Fact] public async Task Cancel_changes_state(){var w=Workflow();_factory.AIWorkflowRepository.GetByIdAsync(w.Id,Arg.Any<CancellationToken>()).Returns(w);(await _client.PostAsync($"/api/v1/workflows/{w.Id}/cancel",null)).StatusCode.Should().Be(HttpStatusCode.OK);w.Status.Should().Be(WorkflowStatus.Cancelled);}
    [Fact] public async Task Pause_and_resume_active_workflow(){var w=Workflow();w.Start("e");_factory.AIWorkflowRepository.GetByIdAsync(w.Id,Arg.Any<CancellationToken>()).Returns(w);(await _client.PostAsync($"/api/v1/workflows/{w.Id}/pause",null)).StatusCode.Should().Be(HttpStatusCode.OK);w.IsPaused.Should().BeTrue();(await _client.PostAsync($"/api/v1/workflows/{w.Id}/resume",null)).StatusCode.Should().Be(HttpStatusCode.OK);w.IsPaused.Should().BeFalse();}
    [Fact] public async Task Retry_failed_workflow_returns_accepted(){var w=Workflow();w.Start("e");w.Fail("failed");_factory.AIWorkflowRepository.GetByIdAsync(w.Id,Arg.Any<CancellationToken>()).Returns(w);(await _client.PostAsync($"/api/v1/workflows/{w.Id}/retry",null)).StatusCode.Should().Be(HttpStatusCode.Accepted);w.Status.Should().NotBe(WorkflowStatus.Failed);}
    [Fact] public async Task Get_for_different_owner_is_forbidden(){var w=AIWorkflow.Create("p","other","w",null,[new WorkflowStep("s",WorkflowStepType.Custom,id:"s")]);_factory.AIWorkflowRepository.GetByIdAsync(w.Id,Arg.Any<CancellationToken>()).Returns(w);(await _client.GetAsync($"/api/v1/workflows/{w.Id}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);}
    [Fact] public async Task Empty_steps_returns_validation_error(){var request=Request([]);(await _client.PostAsJsonAsync("/api/v1/workflows",request)).StatusCode.Should().Be(HttpStatusCode.BadRequest);}

    private static AIWorkflow Workflow()=>AIWorkflow.Create("p","owner","w",null,[new WorkflowStep("s",WorkflowStepType.Custom,id:"s")]);
    private static CreateWorkflowRequest Request(List<WorkflowStepDefinition>? steps=null)=>new(){ProjectId="p",Name="workflow",Steps=steps??[Def("a",[])]};
    private static WorkflowStepDefinition Def(string id,IReadOnlyList<string> deps)=>new(id,id,WorkflowStepType.Custom,deps,null,10,1,null);
}
