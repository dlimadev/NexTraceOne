using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ExecuteAgent;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAgentExecution;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ReviewArtifact;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários para ExecuteAgent, GetAgentExecution, ReviewArtifact.
/// Cobre execução de agents, consulta de execuções e review de artefactos produzidos.
/// </summary>
public sealed class ExecutionArtifactGapsTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IAiAgentRuntimeService _runtimeService = Substitute.For<IAiAgentRuntimeService>();
    private readonly IAiAgentExecutionRepository _executionRepository = Substitute.For<IAiAgentExecutionRepository>();
    private readonly IAiAgentArtifactRepository _artifactRepository = Substitute.For<IAiAgentArtifactRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public ExecutionArtifactGapsTests()
    {
        _currentUser.Id.Returns("user-reviewer");
        _clock.UtcNow.Returns(FixedNow);
    }

    // ── ExecuteAgent ───────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAgent_ValidInput_ReturnsExecutionResult()
    {
        var agentId = Guid.NewGuid();
        var executionId = Guid.NewGuid();

        var runtimeResult = new AgentExecutionResult(
            ExecutionId: executionId,
            AgentId: agentId,
            AgentName: "my-agent",
            Status: "Completed",
            Output: "Agent analysis output",
            PromptTokens: 150,
            CompletionTokens: 80,
            DurationMs: 1200L,
            Artifacts: new List<AgentArtifactResult>());

        _runtimeService.ExecuteAsync(
            Arg.Is<AiAgentId>(x => x == AiAgentId.From(agentId)),
            "Analyze this service",
            null,
            null,
            null,
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<AgentExecutionResult>>(runtimeResult));

        var command = new ExecuteAgent.Command(
            AgentId: agentId,
            Input: "Analyze this service",
            ModelIdOverride: null,
            ContextJson: null,
            TeamId: null);

        var handler = new ExecuteAgent.Handler(_runtimeService);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExecutionId.Should().Be(executionId);
        result.Value.AgentId.Should().Be(agentId);
        result.Value.Status.Should().Be("Completed");
        result.Value.Output.Should().Be("Agent analysis output");
        result.Value.PromptTokens.Should().Be(150);
        result.Value.CompletionTokens.Should().Be(80);
    }

    [Fact]
    public async Task ExecuteAgent_WithModelOverride_PassesOverrideToRuntime()
    {
        var agentId = Guid.NewGuid();
        var modelOverrideId = Guid.NewGuid();

        var runtimeResult = new AgentExecutionResult(
            ExecutionId: Guid.NewGuid(), AgentId: agentId, AgentName: "agent",
            Status: "Completed", Output: "output", PromptTokens: 100, CompletionTokens: 50,
            DurationMs: 500L, Artifacts: new List<AgentArtifactResult>());

        _runtimeService.ExecuteAsync(
            Arg.Any<AiAgentId>(),
            Arg.Any<string>(),
            modelOverrideId,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<AgentExecutionResult>>(runtimeResult));

        var command = new ExecuteAgent.Command(
            AgentId: agentId,
            Input: "Do something",
            ModelIdOverride: modelOverrideId,
            ContextJson: null,
            TeamId: null);

        var handler = new ExecuteAgent.Handler(_runtimeService);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _runtimeService.Received(1).ExecuteAsync(
            Arg.Any<AiAgentId>(),
            "Do something",
            modelOverrideId,
            null,
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAgent_RuntimeFails_ReturnsFailure()
    {
        var agentId = Guid.NewGuid();
        var error = Error.Business("Agent.ExecutionFailed", "Agent could not be executed.");

        _runtimeService.ExecuteAsync(
            Arg.Any<AiAgentId>(),
            Arg.Any<string>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<AgentExecutionResult>>(error));

        var command = new ExecuteAgent.Command(
            AgentId: agentId,
            Input: "Failing input",
            ModelIdOverride: null,
            ContextJson: null,
            TeamId: null);

        var handler = new ExecuteAgent.Handler(_runtimeService);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ExecutionFailed");
    }

    [Fact]
    public async Task ExecuteAgent_WithArtifacts_ReturnsArtifactList()
    {
        var agentId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();

        var runtimeResult = new AgentExecutionResult(
            ExecutionId: Guid.NewGuid(), AgentId: agentId, AgentName: "agent",
            Status: "Completed", Output: "generated contract",
            PromptTokens: 200, CompletionTokens: 300,
            DurationMs: 1500L,
            Artifacts: new List<AgentArtifactResult>
            {
                new(artifactId, "OpenApiSpec", "My Contract", "yaml"),
            });

        _runtimeService.ExecuteAsync(
            Arg.Any<AiAgentId>(),
            Arg.Any<string>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<AgentExecutionResult>>(runtimeResult));

        var command = new ExecuteAgent.Command(
            agentId, "Generate contract", null, null, null);

        var handler = new ExecuteAgent.Handler(_runtimeService);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Artifacts.Should().ContainSingle();
        result.Value.Artifacts[0].ArtifactId.Should().Be(artifactId);
        result.Value.Artifacts[0].Title.Should().Be("My Contract");
    }

    // ── GetAgentExecution ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAgentExecution_ExistingExecution_ReturnsDetails()
    {
        var executionId = Guid.NewGuid();
        var agentId = AiAgentId.New();
        var execution = AiAgentExecution.Start(
            agentId, "user-1", Guid.NewGuid(), "OpenAI",
            inputJson: "{}", contextJson: null, startedAt: FixedNow);

        _executionRepository.GetByIdAsync(
            Arg.Is<AiAgentExecutionId>(x => x == AiAgentExecutionId.From(executionId)),
            Arg.Any<CancellationToken>())
            .Returns(execution);

        _artifactRepository.ListByExecutionAsync(execution.Id, Arg.Any<CancellationToken>())
            .Returns(new List<AiAgentArtifact>().AsReadOnly());

        var handler = new GetAgentExecution.Handler(_executionRepository, _artifactRepository);
        var result = await handler.Handle(new GetAgentExecution.Query(executionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExecutedBy.Should().Be("user-1");
        result.Value.Status.Should().Be("Running");
        result.Value.Artifacts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAgentExecution_NotFound_ReturnsFailure()
    {
        _executionRepository.GetByIdAsync(Arg.Any<AiAgentExecutionId>(), Arg.Any<CancellationToken>())
            .Returns((AiAgentExecution?)null);

        var handler = new GetAgentExecution.Handler(_executionRepository, _artifactRepository);
        var result = await handler.Handle(
            new GetAgentExecution.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AgentExecution.NotFound");
    }

    [Fact]
    public async Task GetAgentExecution_WithArtifacts_ReturnsArtifactList()
    {
        var executionId = Guid.NewGuid();
        var agentId = AiAgentId.New();
        var execution = AiAgentExecution.Start(
            agentId, "user-2", Guid.NewGuid(), "Ollama",
            inputJson: "{}", contextJson: null, startedAt: FixedNow);

        var artifact = AiAgentArtifact.Create(
            execution.Id, agentId,
            AgentArtifactType.OpenApiDraft, "Generated API Contract",
            "openapi: '3.0'", "yaml");

        _executionRepository.GetByIdAsync(
            Arg.Is<AiAgentExecutionId>(x => x == AiAgentExecutionId.From(executionId)),
            Arg.Any<CancellationToken>())
            .Returns(execution);

        _artifactRepository.ListByExecutionAsync(execution.Id, Arg.Any<CancellationToken>())
            .Returns(new List<AiAgentArtifact> { artifact }.AsReadOnly());

        var handler = new GetAgentExecution.Handler(_executionRepository, _artifactRepository);
        var result = await handler.Handle(new GetAgentExecution.Query(executionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Artifacts.Should().ContainSingle();
        result.Value.Artifacts[0].Title.Should().Be("Generated API Contract");
    }

    // ── ReviewArtifact ─────────────────────────────────────────────────────

    [Fact]
    public async Task ReviewArtifact_Approve_UpdatesStatusAndPersists()
    {
        var artifactId = Guid.NewGuid();
        var agentId = AiAgentId.New();
        var executionId = AiAgentExecutionId.New();
        var artifact = AiAgentArtifact.Create(
            executionId, agentId,
            AgentArtifactType.OpenApiDraft, "My Artifact",
            "content", "yaml");

        _artifactRepository.GetByIdAsync(
            Arg.Is<AiAgentArtifactId>(x => x == AiAgentArtifactId.From(artifactId)),
            Arg.Any<CancellationToken>())
            .Returns(artifact);

        var command = new ReviewArtifact.Command(
            ArtifactId: artifactId,
            Decision: "Approve",
            Notes: "Looks good");

        var handler = new ReviewArtifact.Handler(_artifactRepository, _currentUser, _clock);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReviewStatus.Should().Be("Approved");
        await _artifactRepository.Received(1).UpdateAsync(artifact, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReviewArtifact_Reject_UpdatesStatusToRejected()
    {
        var artifactId = Guid.NewGuid();
        var agentId = AiAgentId.New();
        var executionId = AiAgentExecutionId.New();
        var artifact = AiAgentArtifact.Create(
            executionId, agentId,
            AgentArtifactType.OpenApiDraft, "Artifact To Reject",
            "content", "json");

        _artifactRepository.GetByIdAsync(
            Arg.Is<AiAgentArtifactId>(x => x == AiAgentArtifactId.From(artifactId)),
            Arg.Any<CancellationToken>())
            .Returns(artifact);

        var command = new ReviewArtifact.Command(
            ArtifactId: artifactId,
            Decision: "Reject",
            Notes: "Does not meet quality standards");

        var handler = new ReviewArtifact.Handler(_artifactRepository, _currentUser, _clock);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReviewStatus.Should().Be("Rejected");
        await _artifactRepository.Received(1).UpdateAsync(artifact, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReviewArtifact_ArtifactNotFound_ReturnsFailure()
    {
        _artifactRepository.GetByIdAsync(Arg.Any<AiAgentArtifactId>(), Arg.Any<CancellationToken>())
            .Returns((AiAgentArtifact?)null);

        var command = new ReviewArtifact.Command(
            ArtifactId: Guid.NewGuid(),
            Decision: "Approve",
            Notes: null);

        var handler = new ReviewArtifact.Handler(_artifactRepository, _currentUser, _clock);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Artifact.NotFound");
    }

    [Fact]
    public async Task ReviewArtifact_AlreadyReviewed_ReturnsFailure()
    {
        var artifactId = Guid.NewGuid();
        var agentId = AiAgentId.New();
        var executionId = AiAgentExecutionId.New();
        var artifact = AiAgentArtifact.Create(
            executionId, agentId,
            AgentArtifactType.OpenApiDraft, "Already Reviewed",
            "content", "yaml");

        // Approve once to set the state
        artifact.Approve("first-reviewer", FixedNow, "First review");

        _artifactRepository.GetByIdAsync(
            Arg.Is<AiAgentArtifactId>(x => x == AiAgentArtifactId.From(artifactId)),
            Arg.Any<CancellationToken>())
            .Returns(artifact);

        var command = new ReviewArtifact.Command(
            ArtifactId: artifactId,
            Decision: "Reject",
            Notes: "Trying to review again");

        var handler = new ReviewArtifact.Handler(_artifactRepository, _currentUser, _clock);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
