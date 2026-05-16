using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Services;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes de integração para o modo de planeamento do AiAgentRuntimeService.
/// Valida: agent com planning produz PlanSummary, agent sem planning ignora o passo,
/// falha no planning não bloqueia a execução.
/// </summary>
public sealed class PlanExecutionIntegrationTests
{
    private readonly IAiAgentRepository _agentRepository = Substitute.For<IAiAgentRepository>();
    private readonly IAiAgentExecutionRepository _executionRepository = Substitute.For<IAiAgentExecutionRepository>();
    private readonly IAiAgentArtifactRepository _artifactRepository = Substitute.For<IAiAgentArtifactRepository>();
    private readonly IAiExecutionPlanRepository _executionPlanRepository = Substitute.For<IAiExecutionPlanRepository>();
    private readonly IAiModelCatalogService _modelCatalogService = Substitute.For<IAiModelCatalogService>();
    private readonly IAiProviderFactory _providerFactory = Substitute.For<IAiProviderFactory>();
    private readonly IToolRegistry _toolRegistry = Substitute.For<IToolRegistry>();
    private readonly IToolExecutor _toolExecutor = Substitute.For<IToolExecutor>();
    private readonly IToolPermissionValidator _toolPermissionValidator = Substitute.For<IToolPermissionValidator>();
    private readonly IAiTokenQuotaService _tokenQuotaService = Substitute.For<IAiTokenQuotaService>();
    private readonly IContextWindowManager _contextWindow = Substitute.For<IContextWindowManager>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IChatCompletionProvider _chatProvider = Substitute.For<IChatCompletionProvider>();

    private static readonly ResolvedModel DefaultModel = new(
        ModelId: Guid.NewGuid(),
        ModelName: "llama3.2:3b",
        DisplayName: "Llama 3.2 3B",
        ProviderId: "ollama",
        ProviderDisplayName: "Ollama",
        IsInternal: true,
        Capabilities: "chat",
        ContextWindow: 4096);

    private AiAgentRuntimeService CreateSut() => new(
        _agentRepository,
        _executionRepository,
        _artifactRepository,
        _executionPlanRepository,
        _modelCatalogService,
        _providerFactory,
        _toolRegistry,
        _toolExecutor,
        _toolPermissionValidator,
        _tokenQuotaService,
        _contextWindow,
        _currentUser,
        _currentTenant,
        _dateTimeProvider);

    // ── 1. Planning-enabled agent includes plan in result ─────────────────

    [Fact]
    public async Task Execute_PlanningEnabled_Agent_Includes_PlanSummary_In_Result()
    {
        // Arrange
        var agent = CreatePlanningAgent(enablePlanning: true);
        var agentId = agent.Id;

        _agentRepository.GetByIdAsync(agentId, Arg.Any<CancellationToken>()).Returns(agent);
        _modelCatalogService.ResolveDefaultModelAsync("chat", Arg.Any<CancellationToken>())
            .Returns(DefaultModel);
        _providerFactory.GetChatProvider(DefaultModel.ProviderId).Returns(_chatProvider);
        _toolPermissionValidator.GetAllowedTools(Arg.Any<string?>())
            .Returns(new List<ToolDefinition>() as IReadOnlyList<ToolDefinition>);
        _currentUser.Id.Returns("user-test");
        _currentTenant.Id.Returns(Guid.NewGuid());
        _tokenQuotaService.ValidateQuotaAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new TokenQuotaValidationResult(IsAllowed: true));
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _executionRepository.AddAsync(Arg.Any<AiAgentExecution>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _executionRepository.UpdateAsync(Arg.Any<AiAgentExecution>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _agentRepository.UpdateAsync(Arg.Any<AiAgent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _artifactRepository.AddAsync(Arg.Any<AiAgentArtifact>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        const string planText = "1. Analyse input\n2. Generate output\n3. Return result";
        const string mainOutput = "Here is the execution output";

        // First call = plan request, second call = main request
        _chatProvider.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new ChatCompletionResult(true, planText, DefaultModel.ModelId.ToString(), DefaultModel.ProviderId, 10, 50, TimeSpan.Zero),
                new ChatCompletionResult(true, mainOutput, DefaultModel.ModelId.ToString(), DefaultModel.ProviderId, 20, 100, TimeSpan.Zero));

        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteAsync(
            agentId, "Test input", null, null, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PlanSummary.Should().Be(planText);
        result.Value.Output.Should().Be(mainOutput);
    }

    // ── 2. Planning-disabled agent skips planning step ────────────────────

    [Fact]
    public async Task Execute_PlanningDisabled_Agent_Skips_Planning_Step()
    {
        // Arrange
        var agent = CreatePlanningAgent(enablePlanning: false);
        var agentId = agent.Id;

        _agentRepository.GetByIdAsync(agentId, Arg.Any<CancellationToken>()).Returns(agent);
        _modelCatalogService.ResolveDefaultModelAsync("chat", Arg.Any<CancellationToken>())
            .Returns(DefaultModel);
        _providerFactory.GetChatProvider(DefaultModel.ProviderId).Returns(_chatProvider);
        _toolPermissionValidator.GetAllowedTools(Arg.Any<string?>())
            .Returns(new List<ToolDefinition>() as IReadOnlyList<ToolDefinition>);
        _currentUser.Id.Returns("user-test");
        _currentTenant.Id.Returns(Guid.NewGuid());
        _tokenQuotaService.ValidateQuotaAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new TokenQuotaValidationResult(IsAllowed: true));
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _executionRepository.AddAsync(Arg.Any<AiAgentExecution>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _executionRepository.UpdateAsync(Arg.Any<AiAgentExecution>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _agentRepository.UpdateAsync(Arg.Any<AiAgent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _artifactRepository.AddAsync(Arg.Any<AiAgentArtifact>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        const string mainOutput = "Direct output without planning";
        _chatProvider.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult(true, mainOutput, DefaultModel.ModelId.ToString(), DefaultModel.ProviderId, 20, 80, TimeSpan.Zero));

        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteAsync(
            agentId, "Test input", null, null, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PlanSummary.Should().BeNull();
        result.Value.Output.Should().Be(mainOutput);

        // Only 1 CompleteAsync call — the main inference (no planning call)
        await _chatProvider.Received(1).CompleteAsync(
            Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>());
    }

    // ── 3. Planning failure is non-fatal ──────────────────────────────────

    [Fact]
    public async Task Execute_PlanningEnabled_Planning_Failure_Is_NonFatal()
    {
        // Arrange
        var agent = CreatePlanningAgent(enablePlanning: true);
        var agentId = agent.Id;

        _agentRepository.GetByIdAsync(agentId, Arg.Any<CancellationToken>()).Returns(agent);
        _modelCatalogService.ResolveDefaultModelAsync("chat", Arg.Any<CancellationToken>())
            .Returns(DefaultModel);
        _providerFactory.GetChatProvider(DefaultModel.ProviderId).Returns(_chatProvider);
        _toolPermissionValidator.GetAllowedTools(Arg.Any<string?>())
            .Returns(new List<ToolDefinition>() as IReadOnlyList<ToolDefinition>);
        _currentUser.Id.Returns("user-test");
        _currentTenant.Id.Returns(Guid.NewGuid());
        _tokenQuotaService.ValidateQuotaAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new TokenQuotaValidationResult(IsAllowed: true));
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _executionRepository.AddAsync(Arg.Any<AiAgentExecution>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _executionRepository.UpdateAsync(Arg.Any<AiAgentExecution>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _agentRepository.UpdateAsync(Arg.Any<AiAgent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _artifactRepository.AddAsync(Arg.Any<AiAgentArtifact>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        const string mainOutput = "Output despite planning failure";

        // First call (planning) returns failure, second call (main) succeeds
        _chatProvider.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new ChatCompletionResult(false, null, DefaultModel.ModelId.ToString(), DefaultModel.ProviderId, 0, 0, TimeSpan.Zero, "Planning service unavailable"),
                new ChatCompletionResult(true, mainOutput, DefaultModel.ModelId.ToString(), DefaultModel.ProviderId, 20, 80, TimeSpan.Zero));

        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteAsync(
            agentId, "Test input", null, null, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue("execution should succeed even when planning fails");
        result.Value.PlanSummary.Should().BeNull("failed plan should produce null PlanSummary");
        result.Value.Output.Should().Be(mainOutput);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static AiAgent CreatePlanningAgent(bool enablePlanning)
    {
        var agent = AiAgent.Register(
            name: "test-planning-agent",
            displayName: "Test Planning Agent",
            description: "An agent used for planning tests",
            category: AgentCategory.General,
            isOfficial: true,
            systemPrompt: "You are a helpful assistant.");

        if (enablePlanning)
            agent.EnablePlanningMode();

        return agent;
    }
}
