using System.Linq;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Features.ExecuteMcpTool;
using NexTraceOne.AIKnowledge.Application.Runtime.Features.GetMcpServerInfo;
using NexTraceOne.AIKnowledge.Application.Runtime.Features.ListMcpTools;

namespace NexTraceOne.AIKnowledge.Tests.Mcp;

/// <summary>
/// Testes unitários para as features MCP nativas do módulo AIKnowledge.
/// Cobre GetMcpServerInfo, ListMcpTools e ExecuteMcpTool.
/// </summary>
public sealed class McpFeatureTests
{
    // ── GetMcpServerInfo ──────────────────────────────────────────────────

    [Fact]
    public async Task GetMcpServerInfo_Returns_Correct_Protocol_Version()
    {
        var registry = Substitute.For<IToolRegistry>();
        registry.GetAll().Returns(new List<ToolDefinition>
        {
            new("get_service_health", "Get health", "service_catalog", []),
            new("list_services", "List services", "service_catalog", []),
        });

        var handler = new GetMcpServerInfo.Handler(registry);
        var result = await handler.Handle(new GetMcpServerInfo.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProtocolVersion.Should().Be("2024-11-05");
        result.Value.ServerName.Should().Be("NexTraceOne MCP Server");
        result.Value.ServerVersion.Should().Be("1.0.0");
        result.Value.EndpointUrl.Should().Be("/api/v1/ai/mcp");
    }

    [Fact]
    public async Task GetMcpServerInfo_Returns_Correct_ToolCount()
    {
        var registry = Substitute.For<IToolRegistry>();
        registry.GetAll().Returns(new List<ToolDefinition>
        {
            new("tool_a", "Tool A", "cat_a", []),
            new("tool_b", "Tool B", "cat_b", []),
            new("tool_c", "Tool C", "cat_a", []),
        });

        var handler = new GetMcpServerInfo.Handler(registry);
        var result = await handler.Handle(new GetMcpServerInfo.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ToolCount.Should().Be(3);
    }

    [Fact]
    public async Task GetMcpServerInfo_Returns_Distinct_Categories()
    {
        var registry = Substitute.For<IToolRegistry>();
        registry.GetAll().Returns(new List<ToolDefinition>
        {
            new("tool_a", "Tool A", "service_catalog", []),
            new("tool_b", "Tool B", "change_intelligence", []),
            new("tool_c", "Tool C", "service_catalog", []),
        });

        var handler = new GetMcpServerInfo.Handler(registry);
        var result = await handler.Handle(new GetMcpServerInfo.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Categories.Should().HaveCount(2);
        result.Value.Categories.Should().Contain("service_catalog");
        result.Value.Categories.Should().Contain("change_intelligence");
    }

    [Fact]
    public async Task GetMcpServerInfo_With_Empty_Registry_Returns_Zero_Tools()
    {
        var registry = Substitute.For<IToolRegistry>();
        registry.GetAll().Returns(new List<ToolDefinition>());

        var handler = new GetMcpServerInfo.Handler(registry);
        var result = await handler.Handle(new GetMcpServerInfo.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ToolCount.Should().Be(0);
        result.Value.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMcpServerInfo_Capabilities_Has_Tools()
    {
        var registry = Substitute.For<IToolRegistry>();
        registry.GetAll().Returns(new List<ToolDefinition>());

        var handler = new GetMcpServerInfo.Handler(registry);
        var result = await handler.Handle(new GetMcpServerInfo.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Capabilities.Tools.Should().NotBeNull();
        result.Value.Capabilities.Tools!.ListChanged.Should().BeFalse();
        result.Value.Capabilities.Prompts.Should().BeNull();
        result.Value.Capabilities.Resources.Should().BeNull();
    }

    // ── ListMcpTools ──────────────────────────────────────────────────────

    [Fact]
    public async Task ListMcpTools_Returns_All_Tools_When_No_Category_Filter()
    {
        var registry = Substitute.For<IToolRegistry>();
        registry.GetAll().Returns(new List<ToolDefinition>
        {
            new("get_service_health", "Get health", "service_catalog",
                [new ToolParameterDefinition("service_name", "Service name", "string", Required: true)]),
            new("list_recent_changes", "List changes", "change_intelligence", []),
        });

        var handler = new ListMcpTools.Handler(registry);
        var result = await handler.Handle(new ListMcpTools.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Tools.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListMcpTools_Filters_By_Category()
    {
        var registry = Substitute.For<IToolRegistry>();
        registry.GetByCategory("service_catalog").Returns(new List<ToolDefinition>
        {
            new("get_service_health", "Get health", "service_catalog", []),
        });

        var handler = new ListMcpTools.Handler(registry);
        var result = await handler.Handle(
            new ListMcpTools.Query("service_catalog"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Tools.First().Name.Should().Be("get_service_health");
    }

    [Fact]
    public async Task ListMcpTools_Builds_InputSchema_With_Required_Fields()
    {
        var registry = Substitute.For<IToolRegistry>();
        registry.GetAll().Returns(new List<ToolDefinition>
        {
            new("my_tool", "My tool", "cat",
            [
                new ToolParameterDefinition("required_param", "A required param", "string", Required: true),
                new ToolParameterDefinition("optional_param", "An optional param", "boolean", Required: false),
            ]),
        });

        var handler = new ListMcpTools.Handler(registry);
        var result = await handler.Handle(new ListMcpTools.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var tool = result.Value.Tools.First();
        tool.Name.Should().Be("my_tool");
        tool.InputSchema.Type.Should().Be("object");
        tool.InputSchema.Properties.Should().ContainKey("required_param");
        tool.InputSchema.Properties.Should().ContainKey("optional_param");
        tool.InputSchema.Required.Should().Contain("required_param");
        tool.InputSchema.Required.Should().NotContain("optional_param");
    }

    [Fact]
    public async Task ListMcpTools_Maps_Boolean_Type_Correctly()
    {
        var registry = Substitute.For<IToolRegistry>();
        registry.GetAll().Returns(new List<ToolDefinition>
        {
            new("tool_with_bool", "Bool tool", "cat",
            [
                new ToolParameterDefinition("include_slos", "Include SLOs", "boolean"),
            ]),
        });

        var handler = new ListMcpTools.Handler(registry);
        var result = await handler.Handle(new ListMcpTools.Query(), CancellationToken.None);

        var schema = result.Value.Tools.First().InputSchema;
        schema.Properties["include_slos"].Type.Should().Be("boolean");
    }

    [Fact]
    public async Task ListMcpTools_Maps_Integer_Type_Correctly()
    {
        var registry = Substitute.For<IToolRegistry>();
        registry.GetAll().Returns(new List<ToolDefinition>
        {
            new("tool_with_int", "Int tool", "cat",
            [
                new ToolParameterDefinition("max_results", "Max results", "integer"),
            ]),
        });

        var handler = new ListMcpTools.Handler(registry);
        var result = await handler.Handle(new ListMcpTools.Query(), CancellationToken.None);

        var schema = result.Value.Tools.First().InputSchema;
        schema.Properties["max_results"].Type.Should().Be("integer");
    }

    [Fact]
    public async Task ListMcpTools_Returns_Tools_Sorted_By_Name()
    {
        var registry = Substitute.For<IToolRegistry>();
        registry.GetAll().Returns(new List<ToolDefinition>
        {
            new("z_tool", "Z tool", "cat", []),
            new("a_tool", "A tool", "cat", []),
            new("m_tool", "M tool", "cat", []),
        });

        var handler = new ListMcpTools.Handler(registry);
        var result = await handler.Handle(new ListMcpTools.Query(), CancellationToken.None);

        result.Value.Tools.Select(t => t.Name).Should()
            .BeInAscendingOrder();
    }

    // ── ExecuteMcpTool ────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteMcpTool_Returns_Failure_When_Tool_Not_Found()
    {
        var registry = Substitute.For<IToolRegistry>();
        registry.Exists("unknown_tool").Returns(false);
        var executor = Substitute.For<IToolExecutor>();

        var handler = new ExecuteMcpTool.Handler(registry, executor);
        var result = await handler.Handle(
            new ExecuteMcpTool.Command("unknown_tool", "{}"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("AI.Mcp.ToolNotFound");
        await executor.DidNotReceive().ExecuteAsync(
            Arg.Any<ToolCallRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteMcpTool_Executes_Tool_And_Returns_Content()
    {
        var registry = Substitute.For<IToolRegistry>();
        registry.Exists("get_service_health").Returns(true);

        var executor = Substitute.For<IToolExecutor>();
        executor.ExecuteAsync(
            Arg.Is<ToolCallRequest>(r => r.ToolName == "get_service_health"),
            Arg.Any<CancellationToken>())
            .Returns(new ToolExecutionResult(
                true, "get_service_health", """{"status":"healthy"}""", 42));

        var handler = new ExecuteMcpTool.Handler(registry, executor);
        var result = await handler.Handle(
            new ExecuteMcpTool.Command("get_service_health", """{"service_name":"payment"}"""),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsError.Should().BeFalse();
        result.Value.Content.Should().HaveCount(1);
        result.Value.Content.First().Type.Should().Be("text");
        result.Value.Content.First().Text.Should().Contain("healthy");
        result.Value.ToolName.Should().Be("get_service_health");
        result.Value.DurationMs.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteMcpTool_Returns_IsError_True_When_Execution_Fails()
    {
        var registry = Substitute.For<IToolRegistry>();
        registry.Exists("failing_tool").Returns(true);

        var executor = Substitute.For<IToolExecutor>();
        executor.ExecuteAsync(
            Arg.Any<ToolCallRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(new ToolExecutionResult(
                false, "failing_tool", string.Empty, 10, "Connection timeout."));

        var handler = new ExecuteMcpTool.Handler(registry, executor);
        var result = await handler.Handle(
            new ExecuteMcpTool.Command("failing_tool", "{}"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsError.Should().BeTrue();
        result.Value.Content.First().Text.Should().Contain("Connection timeout");
    }

    [Fact]
    public async Task ExecuteMcpTool_Passes_ArgumentsJson_To_Executor()
    {
        const string argsJson = """{"service_name":"order-service","environment":"production"}""";

        var registry = Substitute.For<IToolRegistry>();
        registry.Exists("get_service_health").Returns(true);

        var executor = Substitute.For<IToolExecutor>();
        executor.ExecuteAsync(
            Arg.Any<ToolCallRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(new ToolExecutionResult(true, "get_service_health", "{}", 5));

        var handler = new ExecuteMcpTool.Handler(registry, executor);
        await handler.Handle(
            new ExecuteMcpTool.Command("get_service_health", argsJson),
            CancellationToken.None);

        await executor.Received(1).ExecuteAsync(
            Arg.Is<ToolCallRequest>(r =>
                r.ToolName == "get_service_health" && r.ArgumentsJson == argsJson),
            Arg.Any<CancellationToken>());
    }

    // ── ExecuteMcpTool Validator ──────────────────────────────────────────

    [Fact]
    public void ExecuteMcpTool_Validator_Rejects_Empty_ToolName()
    {
        var validator = new ExecuteMcpTool.Validator();
        var result = validator.Validate(new ExecuteMcpTool.Command("", "{}"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ToolName");
    }

    [Fact]
    public void ExecuteMcpTool_Validator_Accepts_Valid_Command()
    {
        var validator = new ExecuteMcpTool.Validator();
        var result = validator.Validate(
            new ExecuteMcpTool.Command("get_service_health", """{"service_name":"svc"}"""));

        result.IsValid.Should().BeTrue();
    }
}
