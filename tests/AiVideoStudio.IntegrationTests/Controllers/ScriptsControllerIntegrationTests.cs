using System.Net;
using System.Net.Http.Json;
using AiVideoStudio.Application.Features.Scripts.Commands;
using AiVideoStudio.Application.Features.Scripts.DTOs;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.ApiContracts.V1.Scripts.Requests;
using AiVideoStudio.Shared.Responses;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AiVideoStudio.IntegrationTests.Controllers;

public sealed class ScriptsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ScriptsControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ScriptEndpoints_ShouldMapScriptSummarySceneAndElementDtos()
    {
        var userId = $"script-user-{Guid.NewGuid():N}";
        var project = Project.Create("Script mapping project", userId);
        Script? storedScript = null;

        _factory.CurrentUser.IsAuthenticated.Returns(true);
        _factory.CurrentUser.UserId.Returns(userId);
        _factory.CurrentUser.Roles.Returns(new List<string> { "User" });
        _factory.ProjectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _factory.ScriptRepository.AddAsync(Arg.Do<Script>(script => storedScript = script), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _factory.ScriptRepository.GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => storedScript);
        _factory.ScriptRepository.UpdateAsync(Arg.Any<Script>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _factory.ScriptRepository.GetScriptsByProjectAsync(
                project.Id, null, false, null, true, 1, 10, Arg.Any<CancellationToken>())
            .Returns(_ => new PagedResult<Script>(storedScript is null ? [] : [storedScript], storedScript is null ? 0 : 1, 1, 10));

        var createResponse = await _client.PostAsJsonAsync("/api/v1/scripts", new CreateScriptRequest
        {
            ProjectId = project.Id,
            Name = "Mapped script",
            Description = "Mapping regression"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createBody = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ScriptDto>>();
        createBody.Should().NotBeNull();
        createBody!.Data.Should().NotBeNull();
        createBody.Data!.Name.Should().Be("Mapped script");
        createBody.Data.ProjectId.Should().Be(project.Id);
        createBody.Data.Scenes.Should().BeEmpty();
        var scriptId = createBody.Data.Id;

        var sceneResponse = await _client.PostAsJsonAsync($"/api/v1/scripts/{scriptId}/scenes", new AddSceneRequest
        {
            Name = "Opening",
            Duration = TimeSpan.FromSeconds(5),
            Notes = "Mapped notes",
            ExpectedVersion = storedScript!.Version
        });

        sceneResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sceneBody = await sceneResponse.Content.ReadFromJsonAsync<ApiResponse<SceneDto>>();
        sceneBody.Should().NotBeNull();
        sceneBody!.Data.Should().NotBeNull();
        sceneBody.Data!.Name.Should().Be("Opening");
        sceneBody.Data.Elements.Should().BeEmpty();

        var elementResponse = await _client.PostAsJsonAsync(
            $"/api/v1/scripts/{scriptId}/scenes/{sceneBody.Data.Id}/elements",
            new AddSceneElementCommand(scriptId, sceneBody.Data.Id, ElementType.Prompt, "Mapped prompt", "{}", storedScript.Version));

        elementResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var elementBody = await elementResponse.Content.ReadFromJsonAsync<ApiResponse<SceneElementDto>>();
        elementBody.Should().NotBeNull();
        elementBody!.Data.Should().NotBeNull();
        elementBody.Data!.ElementType.Should().Be(ElementType.Prompt);
        elementBody.Data.Content.Should().Be("Mapped prompt");

        var getResponse = await _client.GetAsync($"/api/v1/scripts/{scriptId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getBody = await getResponse.Content.ReadFromJsonAsync<ApiResponse<ScriptDto>>();
        getBody.Should().NotBeNull();
        getBody!.Data.Should().NotBeNull();
        getBody.Data!.Scenes.Should().ContainSingle();
        getBody.Data.Scenes[0].Elements.Should().ContainSingle();
        getBody.Data.Scenes[0].Elements[0].Content.Should().Be("Mapped prompt");

        var listResponse = await _client.GetAsync($"/api/v1/projects/{project.Id}/scripts?pageNumber=1&pageSize=10");

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listBody = await listResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ScriptSummaryDto>>>();
        listBody.Should().NotBeNull();
        listBody!.Data.Should().NotBeNull();
        listBody.Data!.Items.Should().ContainSingle();
        var summary = listBody.Data.Items.Single();
        summary.Id.Should().Be(scriptId);
        summary.Name.Should().Be("Mapped script");
    }
}
