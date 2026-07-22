using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Infrastructure.Mongo.MongoConventions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace AiVideoStudio.UnitTests.Infrastructure;

public sealed class ScriptPersistenceTests
{
    [Fact]
    public void Script_BsonRoundTrip_ShouldPreserveScenesAndElements()
    {
        MongoConventionPackInitializer.Initialize();
        var script = Script.Create("project", "owner", "Script", "Description");
        var scene = script.AddScene("Opening", TimeSpan.FromSeconds(5), "Notes", "owner");
        script.AddSceneElement(scene.Id, ElementType.Prompt, "Prompt content", "{}", "owner");

        var restored = BsonSerializer.Deserialize<Script>(script.ToBson());

        restored.Scenes.Should().ContainSingle();
        var restoredScene = restored.Scenes.Single();
        restoredScene.Name.Should().Be("Opening");
        restoredScene.Elements.Should().ContainSingle();
        restoredScene.Elements.Single().Content.Should().Be("Prompt content");
    }

    [Fact]
    public void Scene_BsonRoundTrip_ShouldPreserveElements()
    {
        MongoConventionPackInitializer.Initialize();
        var script = Script.Create("project", "owner", "Script");
        var scene = script.AddScene("Scene", TimeSpan.FromSeconds(3), null, "owner");
        script.AddSceneElement(scene.Id, ElementType.Voice, "Voice content", null, "owner");

        var restored = BsonSerializer.Deserialize<Scene>(scene.ToBson());

        restored.Elements.Should().ContainSingle();
        var element = restored.Elements.Single();
        element.ElementType.Should().Be(ElementType.Voice);
        element.Content.Should().Be("Voice content");
    }
}
