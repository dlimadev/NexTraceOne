using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.AIKnowledge.Application.Orchestration.Services;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Services;

public sealed class AgentWorkflowOrchestratorTests
{
    private readonly IAiAgentRuntimeService _runtime = Substitute.For<IAiAgentRuntimeService>();
    private readonly IAgentWorkflowExecutionRepository _executionRepo = Substitute.For<IAgentWorkflowExecutionRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<AgentWorkflowOrchestrator> _logger = NullLogger<AgentWorkflowOrchestrator>.Instance;

    public AgentWorkflowOrchestratorTests()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private AgentWorkflowOrchestrator CreateOrchestrator(IWorkflowReplanningService? replanningService = null)
        => new(_runtime, _executionRepo, _clock, _logger, replanningService);

    [Fact]
    public async Task ExecuteSequentialAsync_EmptyWorkflow_ReturnsValidationError()
    {
        var orchestrator = CreateOrchestrator();
        var workflow = new AgentWorkflowDefinition("empty", []);

        var result = await orchestrator.ExecuteSequentialAsync(workflow, "input");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workflow.Empty");
    }

    [Fact]
    public async Task ExecuteSequentialAsync_SingleStep_ReturnsStepOutput()
    {
        var agentId = Guid.NewGuid();
        _runtime.ExecuteAsync(
            Arg.Any<AiAgentId>(),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AgentExecutionResult(
                Guid.NewGuid(), agentId, "SecurityAgent", "Completed",
                "Found 2 vulnerabilities", 100, 50, 200,
                Array.Empty<AgentArtifactResult>()));

        var orchestrator = CreateOrchestrator();
        var workflow = new AgentWorkflowDefinition("sec-review", [new AgentWorkflowStep(agentId)]);

        var result = await orchestrator.ExecuteSequentialAsync(workflow, "review this code");

        result.IsSuccess.Should().BeTrue();
        result.Value.StepResults.Should().HaveCount(1);
        result.Value.StepResults[0].Output.Should().Be("Found 2 vulnerabilities");
        result.Value.StepResults[0].RetryCount.Should().Be(0);
        result.Value.FinalOutput.Should().Be("Found 2 vulnerabilities");

        await _executionRepo.Received(1).AddAsync(Arg.Any<AgentWorkflowExecution>(), Arg.Any<CancellationToken>());
        await _executionRepo.Received(1).UpdateAsync(Arg.Any<AgentWorkflowExecution>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSequentialAsync_TwoSteps_PassesOutputAsInput()
    {
        var agent1 = Guid.NewGuid();
        var agent2 = Guid.NewGuid();

        _runtime.ExecuteAsync(
            Arg.Is<AiAgentId>(a => a.Value == agent1),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AgentExecutionResult(
                Guid.NewGuid(), agent1, "Agent1", "Completed",
                "intermediate result", 10, 5, 100,
                Array.Empty<AgentArtifactResult>()));

        _runtime.ExecuteAsync(
            Arg.Is<AiAgentId>(a => a.Value == agent2),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AgentExecutionResult(
                Guid.NewGuid(), agent2, "Agent2", "Completed",
                "final result", 20, 10, 150,
                Array.Empty<AgentArtifactResult>()));

        var orchestrator = CreateOrchestrator();
        var workflow = new AgentWorkflowDefinition("chain", [
            new AgentWorkflowStep(agent1),
            new AgentWorkflowStep(agent2)
        ]);

        var result = await orchestrator.ExecuteSequentialAsync(workflow, "initial input");

        result.IsSuccess.Should().BeTrue();
        result.Value.StepResults.Should().HaveCount(2);
        result.Value.StepResults[1].Input.Should().Be("intermediate result");
        result.Value.FinalOutput.Should().Be("final result");
    }

    [Fact]
    public async Task ExecuteSequentialAsync_StepFails_ReturnsError()
    {
        var agentId = Guid.NewGuid();
        _runtime.ExecuteAsync(
            Arg.Any<AiAgentId>(),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Error("Agent.Crashed", "LLM timeout", ErrorType.Business));

        var orchestrator = CreateOrchestrator();
        var workflow = new AgentWorkflowDefinition("fail", [new AgentWorkflowStep(agentId)]);

        var result = await orchestrator.ExecuteSequentialAsync(workflow, "input");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workflow.StepFailed");
        result.Error.Message.Should().Contain("LLM timeout");

        await _executionRepo.Received(1).AddAsync(Arg.Any<AgentWorkflowExecution>(), Arg.Any<CancellationToken>());
        await _executionRepo.Received(1).UpdateAsync(Arg.Any<AgentWorkflowExecution>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSequentialAsync_InputTemplate_ReplacesPlaceholder()
    {
        var agentId = Guid.NewGuid();
        _runtime.ExecuteAsync(
            Arg.Any<AiAgentId>(),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AgentExecutionResult(
                Guid.NewGuid(), agentId, "Agent", "Completed",
                "ok", 10, 5, 100,
                Array.Empty<AgentArtifactResult>()));

        var orchestrator = CreateOrchestrator();
        var workflow = new AgentWorkflowDefinition("template", [
            new AgentWorkflowStep(agentId, "Analyze: {previousOutput}")
        ]);

        var result = await orchestrator.ExecuteSequentialAsync(workflow, "code snippet");

        result.IsSuccess.Should().BeTrue();
        await _runtime.Received(1).ExecuteAsync(
            Arg.Any<AiAgentId>(),
            Arg.Is<string>(s => s == "Analyze: code snippet"),
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSequentialAsync_StepFailsThenSucceedsOnRetry_ReturnsSuccess()
    {
        var agentId = Guid.NewGuid();
        var callCount = 0;
        _runtime.ExecuteAsync(
            Arg.Any<AiAgentId>(),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return callCount < 2
                    ? new Error("Agent.Crashed", "Transient error", ErrorType.Business)
                    : new AgentExecutionResult(
                        Guid.NewGuid(), agentId, "Agent", "Completed",
                        "recovered", 10, 5, 100,
                        Array.Empty<AgentArtifactResult>());
            });

        var orchestrator = CreateOrchestrator();
        var workflow = new AgentWorkflowDefinition("retry", [new AgentWorkflowStep(agentId)]);

        var result = await orchestrator.ExecuteSequentialAsync(workflow, "input");

        result.IsSuccess.Should().BeTrue();
        result.Value.StepResults[0].Output.Should().Be("recovered");
        result.Value.StepResults[0].RetryCount.Should().Be(1);
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteSequentialAsync_StepFailsAllRetries_ReturnsErrorWithRetryCount()
    {
        var agentId = Guid.NewGuid();
        _runtime.ExecuteAsync(
            Arg.Any<AiAgentId>(),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Error("Agent.Crashed", "Persistent error", ErrorType.Business));

        var orchestrator = CreateOrchestrator();
        var workflow = new AgentWorkflowDefinition("max-retry", [new AgentWorkflowStep(agentId)]);

        var result = await orchestrator.ExecuteSequentialAsync(workflow, "input");

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Persistent error");
        result.Error.Message.Should().Contain("3 retries");
    }

    [Fact]
    public async Task ExecuteSequentialAsync_ParallelGroup_ExecutesBothAgentsWithSameInput()
    {
        var agent1 = Guid.NewGuid();
        var agent2 = Guid.NewGuid();

        _runtime.ExecuteAsync(
            Arg.Is<AiAgentId>(a => a.Value == agent1),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AgentExecutionResult(
                Guid.NewGuid(), agent1, "SecurityAgent", "Completed",
                "2 vulns found", 10, 5, 100,
                Array.Empty<AgentArtifactResult>()));

        _runtime.ExecuteAsync(
            Arg.Is<AiAgentId>(a => a.Value == agent2),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AgentExecutionResult(
                Guid.NewGuid(), agent2, "PerfAgent", "Completed",
                "p95=120ms", 10, 5, 100,
                Array.Empty<AgentArtifactResult>()));

        var orchestrator = CreateOrchestrator();
        var workflow = new AgentWorkflowDefinition("parallel-review", [
            new AgentWorkflowStep(agent1, ParallelGroupId: 1),
            new AgentWorkflowStep(agent2, ParallelGroupId: 1)
        ]);

        var result = await orchestrator.ExecuteSequentialAsync(workflow, "review service X");

        result.IsSuccess.Should().BeTrue();
        result.Value.StepResults.Should().HaveCount(2);
        result.Value.FinalOutput.Should().Contain("[SecurityAgent]");
        result.Value.FinalOutput.Should().Contain("[PerfAgent]");
        result.Value.FinalOutput.Should().Contain("2 vulns found");
        result.Value.FinalOutput.Should().Contain("p95=120ms");

        // Both agents received the same initial input
        await _runtime.Received(1).ExecuteAsync(
            Arg.Is<AiAgentId>(a => a.Value == agent1),
            Arg.Is<string>(s => s == "review service X"),
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _runtime.Received(1).ExecuteAsync(
            Arg.Is<AiAgentId>(a => a.Value == agent2),
            Arg.Is<string>(s => s == "review service X"),
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSequentialAsync_ParallelThenSequential_CombinesOutputs()
    {
        var agent1 = Guid.NewGuid();
        var agent2 = Guid.NewGuid();
        var agent3 = Guid.NewGuid();

        _runtime.ExecuteAsync(
            Arg.Is<AiAgentId>(a => a.Value == agent1),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AgentExecutionResult(
                Guid.NewGuid(), agent1, "SecurityAgent", "Completed",
                "2 vulns", 10, 5, 100,
                Array.Empty<AgentArtifactResult>()));

        _runtime.ExecuteAsync(
            Arg.Is<AiAgentId>(a => a.Value == agent2),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AgentExecutionResult(
                Guid.NewGuid(), agent2, "PerfAgent", "Completed",
                "p95=120ms", 10, 5, 100,
                Array.Empty<AgentArtifactResult>()));

        _runtime.ExecuteAsync(
            Arg.Is<AiAgentId>(a => a.Value == agent3),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AgentExecutionResult(
                Guid.NewGuid(), agent3, "SummaryAgent", "Completed",
                "All good", 10, 5, 100,
                Array.Empty<AgentArtifactResult>()));

        var orchestrator = CreateOrchestrator();
        var workflow = new AgentWorkflowDefinition("parallel-then-seq", [
            new AgentWorkflowStep(agent1, ParallelGroupId: 1),
            new AgentWorkflowStep(agent2, ParallelGroupId: 1),
            new AgentWorkflowStep(agent3) // sequential after parallel
        ]);

        var result = await orchestrator.ExecuteSequentialAsync(workflow, "review");

        result.IsSuccess.Should().BeTrue();
        result.Value.StepResults.Should().HaveCount(3);
        result.Value.StepResults[2].Input.Should().Contain("[SecurityAgent]");
        result.Value.StepResults[2].Input.Should().Contain("[PerfAgent]");
        result.Value.FinalOutput.Should().Be("All good");
    }

    [Fact]
    public async Task ExecuteSequentialAsync_ParallelGroupOneFails_ReturnsError()
    {
        var agent1 = Guid.NewGuid();
        var agent2 = Guid.NewGuid();

        _runtime.ExecuteAsync(
            Arg.Is<AiAgentId>(a => a.Value == agent1),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AgentExecutionResult(
                Guid.NewGuid(), agent1, "Agent1", "Completed",
                "ok", 10, 5, 100,
                Array.Empty<AgentArtifactResult>()));

        _runtime.ExecuteAsync(
            Arg.Is<AiAgentId>(a => a.Value == agent2),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Error("Agent.Crashed", "LLM timeout", ErrorType.Business));

        var orchestrator = CreateOrchestrator();
        var workflow = new AgentWorkflowDefinition("parallel-fail", [
            new AgentWorkflowStep(agent1, ParallelGroupId: 1),
            new AgentWorkflowStep(agent2, ParallelGroupId: 1)
        ]);

        var result = await orchestrator.ExecuteSequentialAsync(workflow, "input");

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("LLM timeout");
        await _executionRepo.Received(1).UpdateAsync(
            Arg.Is<AgentWorkflowExecution>(e => e.TotalSteps == 2 && e.Status == AgentExecutionStatus.Failed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSequentialAsync_ReplanningDisabled_FailsOnFirstError()
    {
        var agentId = Guid.NewGuid();
        _runtime.ExecuteAsync(
            Arg.Any<AiAgentId>(),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Error("Agent.Failed", "Simulated failure", ErrorType.Business));

        var orchestrator = CreateOrchestrator();
        var workflow = new AgentWorkflowDefinition("fail-fast", [new AgentWorkflowStep(agentId)]);

        var result = await orchestrator.ExecuteSequentialAsync(workflow, "input", enableAdaptiveReplanning: false);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workflow.StepFailed");
    }

    [Fact]
    public async Task ExecuteSequentialAsync_ReplanningSucceeds_ContinuesWithNewPlan()
    {
        var agent1 = Guid.NewGuid();
        var agent2 = Guid.NewGuid();
        var agent3 = Guid.NewGuid();

        // First call fails
        _runtime.ExecuteAsync(
            Arg.Is<AiAgentId>(a => a.Value == agent2),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Error("Agent.Failed", "Step 2 failed", ErrorType.Business));

        // Replanned agent succeeds
        _runtime.ExecuteAsync(
            Arg.Is<AiAgentId>(a => a.Value == agent3),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AgentExecutionResult(
                Guid.NewGuid(), agent3, "Agent3", "Completed",
                "replanned ok", 10, 5, 100,
                Array.Empty<AgentArtifactResult>()));

        var replanner = Substitute.For<IWorkflowReplanningService>();
        replanner.ReplanAsync(
            Arg.Any<AgentWorkflowDefinition>(),
            Arg.Any<IReadOnlyList<AgentWorkflowStepResult>>(),
            Arg.Any<AgentWorkflowStep>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new AgentWorkflowDefinition("replanned", [new AgentWorkflowStep(agent3)]));

        var orchestrator = CreateOrchestrator(replanner);
        var workflow = new AgentWorkflowDefinition("original", [
            new AgentWorkflowStep(agent1),
            new AgentWorkflowStep(agent2)
        ]);

        // Agent1 must succeed for the workflow to reach agent2
        _runtime.ExecuteAsync(
            Arg.Is<AiAgentId>(a => a.Value == agent1),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AgentExecutionResult(
                Guid.NewGuid(), agent1, "Agent1", "Completed",
                "step1 ok", 10, 5, 100,
                Array.Empty<AgentArtifactResult>()));

        var result = await orchestrator.ExecuteSequentialAsync(workflow, "input", enableAdaptiveReplanning: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.StepResults.Should().Contain(s => s.AgentId == agent1 && s.Success);
        result.Value.StepResults.Should().Contain(s => s.AgentId == agent3 && s.Output == "replanned ok");
    }

    [Fact]
    public async Task ExecuteSequentialAsync_ReplanningReturnsNull_Fails()
    {
        var agentId = Guid.NewGuid();
        _runtime.ExecuteAsync(
            Arg.Any<AiAgentId>(),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new Error("Agent.Failed", "Failure", ErrorType.Business));

        var replanner = Substitute.For<IWorkflowReplanningService>();
        replanner.ReplanAsync(
            Arg.Any<AgentWorkflowDefinition>(),
            Arg.Any<IReadOnlyList<AgentWorkflowStepResult>>(),
            Arg.Any<AgentWorkflowStep>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns((AgentWorkflowDefinition?)null);

        var orchestrator = CreateOrchestrator(replanner);
        var workflow = new AgentWorkflowDefinition("fail", [new AgentWorkflowStep(agentId)]);

        var result = await orchestrator.ExecuteSequentialAsync(workflow, "input", enableAdaptiveReplanning: true);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workflow.StepFailed");
    }

    [Fact]
    public async Task ExecuteSequentialAsync_StepWithCustomTimeout_TimesOut()
    {
        var agentId = Guid.NewGuid();
        _runtime.ExecuteAsync(
            Arg.Any<AiAgentId>(),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ct = ci.Arg<CancellationToken>();
                var tcs = new TaskCompletionSource<Result<AgentExecutionResult>>();
                ct.Register(() => tcs.TrySetException(new OperationCanceledException()));
                return tcs.Task;
            });

        var orchestrator = CreateOrchestrator();
        var workflow = new AgentWorkflowDefinition("timeout", [
            new AgentWorkflowStep(agentId, StepTimeoutSeconds: 1)
        ]);

        var result = await orchestrator.ExecuteSequentialAsync(workflow, "input");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workflow.StepFailed");
        result.Error.Message.Should().Contain("timed out after 1s");
    }

    [Fact]
    public async Task ExecuteSequentialAsync_StepTimeoutRetriesUntilMaxRetries_ReturnsError()
    {
        var agentId = Guid.NewGuid();
        _runtime.ExecuteAsync(
            Arg.Any<AiAgentId>(),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ct = ci.Arg<CancellationToken>();
                var tcs = new TaskCompletionSource<Result<AgentExecutionResult>>();
                ct.Register(() => tcs.TrySetException(new OperationCanceledException()));
                return tcs.Task;
            });

        var orchestrator = CreateOrchestrator();
        var workflow = new AgentWorkflowDefinition("timeout-retry", [
            new AgentWorkflowStep(agentId, StepTimeoutSeconds: 1)
        ]);

        var result = await orchestrator.ExecuteSequentialAsync(workflow, "input");

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("timed out after 1s");
    }

    [Fact]
    public async Task ExecuteSequentialAsync_ParallelGroupTimesOut_ReturnsError()
    {
        var agent1 = Guid.NewGuid();
        var agent2 = Guid.NewGuid();

        _runtime.ExecuteAsync(
            Arg.Any<AiAgentId>(),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ct = ci.Arg<CancellationToken>();
                var tcs = new TaskCompletionSource<Result<AgentExecutionResult>>();
                ct.Register(() => tcs.TrySetException(new OperationCanceledException()));
                return tcs.Task;
            });

        var orchestrator = CreateOrchestrator();
        var workflow = new AgentWorkflowDefinition("parallel-timeout", [
            new AgentWorkflowStep(agent1, ParallelGroupId: 1, StepTimeoutSeconds: 1),
            new AgentWorkflowStep(agent2, ParallelGroupId: 1, StepTimeoutSeconds: 1)
        ]);

        var result = await orchestrator.ExecuteSequentialAsync(workflow, "input");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workflow.StepFailed");
        result.Error.Message.Should().Contain("timed out after 1s");
    }

    [Fact]
    public async Task ExecuteSequentialAsync_StepWithDefaultTimeout_DoesNotTimeOutIfFast()
    {
        var agentId = Guid.NewGuid();
        _runtime.ExecuteAsync(
            Arg.Any<AiAgentId>(),
            Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AgentExecutionResult(
                Guid.NewGuid(), agentId, "Agent", "Completed",
                "fast result", 10, 5, 100,
                Array.Empty<AgentArtifactResult>()));

        var orchestrator = CreateOrchestrator();
        var workflow = new AgentWorkflowDefinition("fast", [new AgentWorkflowStep(agentId)]);

        var result = await orchestrator.ExecuteSequentialAsync(workflow, "input");

        result.IsSuccess.Should().BeTrue();
        result.Value.FinalOutput.Should().Be("fast result");
    }
}
