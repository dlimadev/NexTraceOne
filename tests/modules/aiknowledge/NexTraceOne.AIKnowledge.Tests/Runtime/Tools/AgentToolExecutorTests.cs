using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Tools;

/// <summary>Testes unitários do AgentToolExecutor.</summary>
public sealed class AgentToolExecutorTests
{
    private readonly ILogger<AgentToolExecutor> _logger = Substitute.For<ILogger<AgentToolExecutor>>();

    private IAgentTool CreateMockTool(string name, ToolExecutionResult result)
    {
        var tool = Substitute.For<IAgentTool>();
        tool.Definition.Returns(new ToolDefinition(
            name, $"Test: {name}", "test", []));
        tool.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(result);
        return tool;
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExecuteRegisteredTool()
    {
        var expectedResult = new ToolExecutionResult(true, "my_tool", "{\"data\":1}", 5);
        var tool = CreateMockTool("my_tool", expectedResult);
        var executor = new AgentToolExecutor([tool], _logger);

        var result = await executor.ExecuteAsync(
            new ToolCallRequest("my_tool", "{}"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ToolName.Should().Be("my_tool");
        result.Output.Should().Be("{\"data\":1}");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnError_WhenToolNotFound()
    {
        var executor = new AgentToolExecutor([], _logger);

        var result = await executor.ExecuteAsync(
            new ToolCallRequest("nonexistent", "{}"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not registered");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCatchException_AndReturnError()
    {
        var tool = Substitute.For<IAgentTool>();
        tool.Definition.Returns(new ToolDefinition("fail_tool", "Fails", "test", []));
        tool.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<ToolExecutionResult>(_ => throw new InvalidOperationException("boom"));

        var executor = new AgentToolExecutor([tool], _logger);

        var result = await executor.ExecuteAsync(
            new ToolCallRequest("fail_tool", "{}"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("boom");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBeCaseInsensitive()
    {
        var expectedResult = new ToolExecutionResult(true, "My_Tool", "{}", 1);
        var tool = CreateMockTool("My_Tool", expectedResult);
        var executor = new AgentToolExecutor([tool], _logger);

        var result = await executor.ExecuteAsync(
            new ToolCallRequest("my_tool", "{}"), CancellationToken.None);

        result.Success.Should().BeTrue();
    }
}
