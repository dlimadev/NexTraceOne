using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.ExecuteAgentWorkflow;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Tests.Orchestration.Features;

public sealed class ExecuteAgentWorkflowTests
{
    private readonly IAgentWorkflowOrchestrator _orchestrator = Substitute.For<IAgentWorkflowOrchestrator>();

    private ExecuteAgentWorkflow.Handler CreateHandler() => new(_orchestrator);

    [Fact]
    public async Task Handle_SingleAgent_ReturnsResponse()
    {
        var agentId = Guid.NewGuid();
        var workflowResult = new AgentWorkflowResult(
            true,
            [new AgentWorkflowStepResult(agentId, "SecurityAgent", "input", "output", 100, true)],
            "output");

        _orchestrator.ExecuteSequentialAsync(
            Arg.Any<AgentWorkflowDefinition>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<AgentWorkflowResult>.Success(workflowResult));

        var handler = CreateHandler();
        var command = new ExecuteAgentWorkflow.Command("test", [agentId], "initial input");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.Steps.Should().HaveCount(1);
        result.Value.FinalOutput.Should().Be("output");
    }

    [Fact]
    public async Task Handle_OrchestratorFails_ReturnsError()
    {
        var agentId = Guid.NewGuid();
        _orchestrator.ExecuteSequentialAsync(
            Arg.Any<AgentWorkflowDefinition>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(new Error("Workflow.Failed", "Step failed", ErrorType.Business));

        var handler = CreateHandler();
        var command = new ExecuteAgentWorkflow.Command("test", [agentId], "initial input");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workflow.Failed");
    }

    [Fact]
    public async Task Handle_MultipleAgents_BuildsCorrectWorkflow()
    {
        var agent1 = Guid.NewGuid();
        var agent2 = Guid.NewGuid();
        var workflowResult = new AgentWorkflowResult(
            true,
            [
                new AgentWorkflowStepResult(agent1, "Agent1", "input", "mid", 50, true),
                new AgentWorkflowStepResult(agent2, "Agent2", "mid", "final", 50, true)
            ],
            "final");

        _orchestrator.ExecuteSequentialAsync(
            Arg.Any<AgentWorkflowDefinition>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<AgentWorkflowResult>.Success(workflowResult));

        var handler = CreateHandler();
        var command = new ExecuteAgentWorkflow.Command("chain", [agent1, agent2], "start");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Steps.Should().HaveCount(2);

        await _orchestrator.Received(1).ExecuteSequentialAsync(
            Arg.Is<AgentWorkflowDefinition>(w => w.Steps.Count == 2
                && w.Steps[0].AgentId == agent1
                && w.Steps[1].AgentId == agent2),
            Arg.Is<string>(s => s == "start"),
            Arg.Is<string?>(s => s == null),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }
}
