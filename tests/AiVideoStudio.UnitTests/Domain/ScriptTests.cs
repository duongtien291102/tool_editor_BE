using System;
using System.Linq;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace AiVideoStudio.UnitTests.DomainTests;

public class ScriptTests
{
    [Fact]
    public void CreateScript_Should_Initialize_Correctly()
    {
        var script = Script.Create("project-1", "user-1", "Test Script", "Test Desc");

        script.ProjectId.Should().Be("project-1");
        script.OwnerId.Should().Be("user-1");
        script.Name.Should().Be("Test Script");
        script.Description.Should().Be("Test Desc");
        script.Version.Should().Be(1);
        script.Scenes.Should().BeEmpty();
    }

    [Fact]
    public void AddScene_Should_Increase_Version_And_Order_Correctly()
    {
        var script = Script.Create("project-1", "user-1", "Test Script", "Test Desc");
        
        var scene1 = script.AddScene("Scene 1", TimeSpan.FromSeconds(10), "Notes 1", "user-1");
        
        script.Version.Should().Be(2);
        scene1.Order.Should().Be(1);
        script.Scenes.Should().HaveCount(1);
        
        var scene2 = script.AddScene("Scene 2", TimeSpan.FromSeconds(5), "Notes 2", "user-1");
        
        script.Version.Should().Be(3);
        scene2.Order.Should().Be(2);
        script.Scenes.Should().HaveCount(2);
    }
}
