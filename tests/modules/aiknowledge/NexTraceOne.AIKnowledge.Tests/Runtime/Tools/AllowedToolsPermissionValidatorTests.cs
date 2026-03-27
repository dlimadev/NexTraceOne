using System.Linq;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Tools;

/// <summary>Testes unitários do AllowedToolsPermissionValidator.</summary>
public sealed class AllowedToolsPermissionValidatorTests
{
    private static IToolRegistry CreateRegistryWithTools(params string[] toolNames)
    {
        var tools = toolNames.Select(name =>
        {
            var tool = Substitute.For<IAgentTool>();
            tool.Definition.Returns(new ToolDefinition(
                name, $"Test: {name}", "test",
                [new ToolParameterDefinition("p", "desc", "string")]));
            return tool;
        });
        return new InMemoryToolRegistry(tools);
    }

    [Fact]
    public void IsToolAllowed_ShouldReturnTrue_WhenToolInCsv()
    {
        var registry = CreateRegistryWithTools("list_services", "get_health");
        var validator = new AllowedToolsPermissionValidator(registry);

        validator.IsToolAllowed("list_services,get_health", "list_services")
            .Should().BeTrue();
    }

    [Fact]
    public void IsToolAllowed_ShouldReturnFalse_WhenToolNotInCsv()
    {
        var registry = CreateRegistryWithTools("list_services");
        var validator = new AllowedToolsPermissionValidator(registry);

        validator.IsToolAllowed("list_services", "get_health")
            .Should().BeFalse();
    }

    [Fact]
    public void IsToolAllowed_ShouldReturnFalse_WhenCsvIsEmpty()
    {
        var registry = CreateRegistryWithTools("list_services");
        var validator = new AllowedToolsPermissionValidator(registry);

        validator.IsToolAllowed("", "list_services").Should().BeFalse();
        validator.IsToolAllowed(null!, "list_services").Should().BeFalse();
    }

    [Fact]
    public void IsToolAllowed_ShouldBeCaseInsensitive()
    {
        var registry = CreateRegistryWithTools("list_services");
        var validator = new AllowedToolsPermissionValidator(registry);

        validator.IsToolAllowed("List_Services", "list_services")
            .Should().BeTrue();
    }

    [Fact]
    public void GetAllowedTools_ShouldReturnFilteredTools()
    {
        var registry = CreateRegistryWithTools("list_services", "get_health", "list_changes");
        var validator = new AllowedToolsPermissionValidator(registry);

        var allowed = validator.GetAllowedTools("list_services,list_changes");

        allowed.Should().HaveCount(2);
        allowed.Select(t => t.Name).Should().Contain("list_services");
        allowed.Select(t => t.Name).Should().Contain("list_changes");
    }

    [Fact]
    public void GetAllowedTools_ShouldReturnEmpty_WhenNoneMatch()
    {
        var registry = CreateRegistryWithTools("list_services");
        var validator = new AllowedToolsPermissionValidator(registry);

        var allowed = validator.GetAllowedTools("nonexistent_tool");

        allowed.Should().BeEmpty();
    }

    [Fact]
    public void GetAllowedTools_ShouldReturnEmpty_WhenCsvIsEmpty()
    {
        var registry = CreateRegistryWithTools("list_services");
        var validator = new AllowedToolsPermissionValidator(registry);

        validator.GetAllowedTools("").Should().BeEmpty();
    }

    [Fact]
    public void IsToolAllowed_ShouldHandleWhitespace()
    {
        var registry = CreateRegistryWithTools("list_services", "get_health");
        var validator = new AllowedToolsPermissionValidator(registry);

        validator.IsToolAllowed(" list_services , get_health ", "list_services")
            .Should().BeTrue();
    }
}
