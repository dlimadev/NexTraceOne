using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Tools;

/// <summary>Testes unitários do InMemoryToolRegistry.</summary>
public sealed class InMemoryToolRegistryTests
{
    private static IAgentTool CreateMockTool(string name, string category = "test")
    {
        var tool = Substitute.For<IAgentTool>();
        tool.Definition.Returns(new ToolDefinition(
            name, $"Test tool: {name}", category,
            [new ToolParameterDefinition("param1", "A test param", "string")]));
        return tool;
    }

    [Fact]
    public void GetByName_ShouldReturnTool_WhenExists()
    {
        var tool = CreateMockTool("list_services", "service_catalog");
        var registry = new InMemoryToolRegistry([tool]);

        var result = registry.GetByName("list_services");

        result.Should().NotBeNull();
        result!.Name.Should().Be("list_services");
    }

    [Fact]
    public void GetByName_ShouldReturnNull_WhenNotExists()
    {
        var registry = new InMemoryToolRegistry([]);

        var result = registry.GetByName("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public void GetByName_ShouldBeCaseInsensitive()
    {
        var tool = CreateMockTool("List_Services");
        var registry = new InMemoryToolRegistry([tool]);

        var result = registry.GetByName("list_services");

        result.Should().NotBeNull();
    }

    [Fact]
    public void GetAll_ShouldReturnAllRegisteredTools()
    {
        var t1 = CreateMockTool("tool_a");
        var t2 = CreateMockTool("tool_b");
        var registry = new InMemoryToolRegistry([t1, t2]);

        var result = registry.GetAll();

        result.Should().HaveCount(2);
    }

    [Fact]
    public void GetByCategory_ShouldFilterByCategory()
    {
        var t1 = CreateMockTool("svc_tool", "service_catalog");
        var t2 = CreateMockTool("chg_tool", "change_intelligence");
        var registry = new InMemoryToolRegistry([t1, t2]);

        var result = registry.GetByCategory("service_catalog");

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("svc_tool");
    }

    [Fact]
    public void Exists_ShouldReturnTrue_WhenToolRegistered()
    {
        var tool = CreateMockTool("my_tool");
        var registry = new InMemoryToolRegistry([tool]);

        registry.Exists("my_tool").Should().BeTrue();
        registry.Exists("missing_tool").Should().BeFalse();
    }
}
