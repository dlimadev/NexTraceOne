using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.SendAssistantMessage;
using NexTraceOne.AIKnowledge.Application.Governance.Services;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários para a integração LLM no handler SendAssistantMessage.
/// Valida que o handler invoca IChatCompletionProvider diretamente, usa os token counts
/// reais do ChatCompletionResult, faz degradação graciosa em caso de falha do provider,
/// e usa o routing port como fallback quando o provider não está registado localmente.
/// </summary>
public sealed class SendAssistantMessageLlmTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
    private const string OllamaProviderId = "ollama";
    private const string ModelId = "qwen3.5:9b";

    // ── Mocks ────────────────────────────────────────────────────────────
    private readonly IAiUsageEntryRepository _usageRepo = Substitute.For<IAiUsageEntryRepository>();
    private readonly IAiRoutingStrategyRepository _routingRepo = Substitute.For<IAiRoutingStrategyRepository>();
    private readonly IAiKnowledgeSourceRepository _sourceRepo = Substitute.For<IAiKnowledgeSourceRepository>();
    private readonly IAiModelAuthorizationService _modelAuth = Substitute.For<IAiModelAuthorizationService>();
    private readonly IAiTokenQuotaService _quotaService = Substitute.For<IAiTokenQuotaService>();
    private readonly IExternalAIRoutingPort _routingPort = Substitute.For<IExternalAIRoutingPort>();
    private readonly IAiProviderFactory _providerFactory = Substitute.For<IAiProviderFactory>();
    private readonly IContextGroundingService _groundingService = Substitute.For<IContextGroundingService>();
    private readonly IAiRoutingResolver _routingResolver = Substitute.For<IAiRoutingResolver>();
    private readonly IConversationPersistenceService _convPersistence = Substitute.For<IConversationPersistenceService>();
    private readonly IAiGuardrailEnforcementService _guardrailService = Substitute.For<IAiGuardrailEnforcementService>();
    private readonly IContextWindowManager _contextWindow = Substitute.For<IContextWindowManager>();
    private readonly IPromptCacheService _promptCache = Substitute.For<IPromptCacheService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly ICurrentEnvironment _currentEnvironment = Substitute.For<ICurrentEnvironment>();

    public SendAssistantMessageLlmTests()
    {
        _currentUser.Id.Returns("user-001");
        _currentUser.Email.Returns("engineer@nextraceone.io");
        _currentUser.Name.Returns("Test Engineer");
        _currentUser.IsAuthenticated.Returns(true);
        _currentTenant.Id.Returns(Guid.NewGuid());

        // Quota always allowed by default
        _quotaService.ValidateQuotaAsync(
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new TokenQuotaValidationResult(IsAllowed: true));

        // Guardrails always pass by default
        _guardrailService.EvaluateInputAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(GuardrailEvaluationResult.Passed());
        _guardrailService.EvaluateOutputAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(GuardrailEvaluationResult.Passed());

        // Default routing strategy repo
        _routingRepo.ListAsync(true, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NexTraceOne.AIKnowledge.Domain.Governance.Entities.AIRoutingStrategy>().AsReadOnly());

        // Default source repo
        _sourceRepo.ListAsync(null, true, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NexTraceOne.AIKnowledge.Domain.Governance.Entities.AIKnowledgeSource>().AsReadOnly());

        // Default grounding service — reflects actual persona and contextScope
        _groundingService.ResolveGroundingAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<Guid?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(),
            Arg.Any<string?>(), Arg.Any<IReadOnlyList<NexTraceOne.AIKnowledge.Domain.Governance.Entities.AIKnowledgeSource>>(),
            Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var personaArg = callInfo.ArgAt<string>(1);
                var contextScopeArg = callInfo.ArgAt<string?>(2) ?? "general";
                return new GroundingResolutionResult(
                    GroundingContext: $"Persona: {personaArg}\nContextScope: {contextScopeArg}",
                    SystemPrompt: $"You are NexTraceOne AI Assistant. Context scope: {contextScopeArg}\n\n## Grounding Context\nPersona: {personaArg}\nContextScope: {contextScopeArg}",
                    GroundingSources: ["Service Catalog", "Contract Registry"],
                    ContextSummary: null,
                    SuggestedSteps: null,
                    Caveats: null,
                    ContextStrength: "none",
                    ConfidenceLevel: "Unknown",
                    SourceWeightingSummary: "no-matches",
                    UseCaseType: AIUseCaseType.General);
            });

        // Default routing resolver
        _routingResolver.ResolveRoutingAsync(
            Arg.Any<string>(), Arg.Any<AIUseCaseType>(), Arg.Any<string>(), Arg.Any<Guid?>(),
            Arg.Any<string>(), Arg.Any<IReadOnlyList<NexTraceOne.AIKnowledge.Domain.Governance.Entities.AIRoutingStrategy>>(),
            Arg.Any<CancellationToken>())
            .Returns(new RoutingResolutionResult(
                SelectedModel: string.Empty,
                SelectedProvider: string.Empty,
                IsInternal: true,
                RoutingPath: AIRoutingPath.InternalOnly,
                RoutingRationale: "Test routing",
                CostClass: "low",
                EscalationReason: "None",
                AppliedStrategy: null));

        // Default conversation persistence
        SetupDefaultConversationPersistence();
    }

    private void SetupDefaultConversationPersistence()
    {
        var convId = Guid.NewGuid();
        var conversation = NexTraceOne.AIKnowledge.Domain.Governance.Entities.AiAssistantConversation.Start(
            "Test", "Engineer",
            NexTraceOne.AIKnowledge.Domain.Governance.Enums.AIClientType.Web,
            "general", "user-001");

        _convPersistence.GetOrCreateAsync(
            Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<NexTraceOne.AIKnowledge.Domain.Governance.Enums.AIClientType>(),
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns((conversation, (NexTraceOne.BuildingBlocks.Core.Results.Error?)null));

        _convPersistence.PersistMessagePairAsync(
            Arg.Any<NexTraceOne.AIKnowledge.Domain.Governance.Entities.AiAssistantConversation>(),
            Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var promptTokens = callInfo.ArgAt<int>(7);
                var completionTokens = callInfo.ArgAt<int>(8);
                var isDegradedArg = callInfo.ArgAt<bool>(13);
                return new MessagePersistenceResult(
                    convId, Guid.NewGuid(), FixedNow, Guid.NewGuid(), FixedNow,
                    isDegradedArg ? "Degraded" : "Ready",
                    isDegradedArg, isDegradedArg ? "Provider unavailable" : null,
                    "Test conversation", 2, FixedNow);
            });
    }

    private SendAssistantMessage.Handler CreateHandler() => new(
        _usageRepo,
        _routingRepo,
        _sourceRepo,
        _modelAuth,
        _quotaService,
        _routingPort,
        _providerFactory,
        _groundingService,
        _routingResolver,
        _convPersistence,
        _guardrailService,
        _contextWindow,
        _promptCache,
        _currentUser,
        _currentTenant,
        _currentEnvironment,
        NullLogger<SendAssistantMessage.Handler>.Instance);

    private IChatCompletionProvider MockProvider(string content, int promptTokens = 50, int completionTokens = 120)
    {
        var provider = Substitute.For<IChatCompletionProvider>();
        provider.ProviderId.Returns(OllamaProviderId);
        provider.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult(
                Success: true,
                Content: content,
                ModelId: ModelId,
                ProviderId: OllamaProviderId,
                PromptTokens: promptTokens,
                CompletionTokens: completionTokens,
                Duration: TimeSpan.FromMilliseconds(300)));
        _providerFactory.GetChatProvider(Arg.Any<string>()).Returns(provider);
        return provider;
    }

    // ── Test 1: successful LLM call returns AI-generated content ─────────

    [Fact]
    public async Task Handle_WhenProviderAvailable_ReturnsAiGeneratedContent()
    {
        const string aiResponse = "The service health is nominal. No anomalies detected.";
        MockProvider(aiResponse);

        var result = await CreateHandler().Handle(
            new SendAssistantMessage.Command(
                ConversationId: null,
                Message: "What is the health of the payment service?",
                ContextScope: "services",
                Persona: "Engineer",
                PreferredModelId: null,
                ClientType: "Web",
                ServiceId: null, ContractId: null, IncidentId: null,
                ChangeId: null, TeamId: null, DomainId: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AssistantResponse.Should().Be(aiResponse);
        result.Value.IsDegraded.Should().BeFalse();
    }

    // ── Test 2: real token counts captured from ChatCompletionResult ─────

    [Fact]
    public async Task Handle_WhenProviderAvailable_UsesRealTokenCounts()
    {
        const int expectedPrompt = 87;
        const int expectedCompletion = 213;
        MockProvider("Token-precise response.", promptTokens: expectedPrompt, completionTokens: expectedCompletion);

        var result = await CreateHandler().Handle(
            new SendAssistantMessage.Command(
                ConversationId: null,
                Message: "Explain the last change.",
                ContextScope: "changes",
                Persona: "TechLead",
                PreferredModelId: null,
                ClientType: "Web",
                ServiceId: null, ContractId: null, IncidentId: null,
                ChangeId: null, TeamId: null, DomainId: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PromptTokens.Should().Be(expectedPrompt);
        result.Value.CompletionTokens.Should().Be(expectedCompletion);
    }

    // ── Test 3: provider failure triggers graceful degradation ───────────

    [Fact]
    public async Task Handle_WhenProviderThrows_ReturnsDegradedResponse()
    {
        var provider = Substitute.For<IChatCompletionProvider>();
        provider.ProviderId.Returns(OllamaProviderId);
        provider.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns<ChatCompletionResult>(_ => throw new InvalidOperationException("Connection refused"));
        _providerFactory.GetChatProvider(Arg.Any<string>()).Returns(provider);

        var result = await CreateHandler().Handle(
            new SendAssistantMessage.Command(
                ConversationId: null,
                Message: "Summarize recent incidents.",
                ContextScope: "incidents",
                Persona: "Engineer",
                PreferredModelId: null,
                ClientType: "Web",
                ServiceId: null, ContractId: null, IncidentId: null,
                ChangeId: null, TeamId: null, DomainId: null),
            CancellationToken.None);

        // Should succeed with degraded response, not throw
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDegraded.Should().BeTrue();
        result.Value.AssistantResponse.Should().NotBeNullOrWhiteSpace();
    }

    // ── Test 4: factory finds no provider → falls back to routing port ───

    [Fact]
    public async Task Handle_WhenNoLocalProvider_FallsBackToRoutingPort()
    {
        const string routedResponse = "Routed response from external gateway.";
        _providerFactory.GetChatProvider(Arg.Any<string>()).Returns((IChatCompletionProvider?)null);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(routedResponse);

        var result = await CreateHandler().Handle(
            new SendAssistantMessage.Command(
                ConversationId: null,
                Message: "What contracts are at risk?",
                ContextScope: "contracts",
                Persona: "Architect",
                PreferredModelId: null,
                ClientType: "Web",
                ServiceId: null, ContractId: null, IncidentId: null,
                ChangeId: null, TeamId: null, DomainId: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AssistantResponse.Should().Be(routedResponse);
        await _routingPort.Received(1).RouteQueryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    // ── Test 5: model ID and provider ID from result are reflected in response

    [Fact]
    public async Task Handle_WhenProviderAvailable_ReflectsModelAndProviderInResponse()
    {
        MockProvider("Model details response.", promptTokens: 30, completionTokens: 60);

        var result = await CreateHandler().Handle(
            new SendAssistantMessage.Command(
                ConversationId: null,
                Message: "Which model am I using?",
                ContextScope: null,
                Persona: "Engineer",
                PreferredModelId: null,
                ClientType: "Web",
                ServiceId: null, ContractId: null, IncidentId: null,
                ChangeId: null, TeamId: null, DomainId: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ModelUsed.Should().Be(ModelId);
        result.Value.Provider.Should().Be(OllamaProviderId);
        result.Value.IsDegraded.Should().BeFalse();
    }

    // ── Test 6: system prompt contains persona and context scope ─────────

    [Fact]
    public async Task Handle_BuildsSystemPrompt_IncludingPersonaAndContextScope()
    {
        const string persona = "Auditor";
        const string contextScope = "compliance";
        ChatCompletionRequest? capturedRequest = null;

        var provider = Substitute.For<IChatCompletionProvider>();
        provider.ProviderId.Returns(OllamaProviderId);
        provider.CompleteAsync(Arg.Do<ChatCompletionRequest>(r => capturedRequest = r), Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult(true, "Compliance context.", ModelId, OllamaProviderId, 10, 20, TimeSpan.Zero));
        _providerFactory.GetChatProvider(Arg.Any<string>()).Returns(provider);

        await CreateHandler().Handle(
            new SendAssistantMessage.Command(
                ConversationId: null,
                Message: "Show compliance posture.",
                ContextScope: contextScope,
                Persona: persona,
                PreferredModelId: null,
                ClientType: "Web",
                ServiceId: null, ContractId: null, IncidentId: null,
                ChangeId: null, TeamId: null, DomainId: null),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.SystemPrompt.Should().Contain(persona);
        capturedRequest.SystemPrompt.Should().Contain(contextScope);
    }

    // ── Test 7: provider empty response triggers degradation ─────────────

    [Fact]
    public async Task Handle_WhenProviderReturnsEmptyContent_TriggersDegradation()
    {
        var provider = Substitute.For<IChatCompletionProvider>();
        provider.ProviderId.Returns(OllamaProviderId);
        provider.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult(
                Success: false, Content: null, ModelId: ModelId,
                ProviderId: OllamaProviderId,
                PromptTokens: 0, CompletionTokens: 0,
                Duration: TimeSpan.Zero,
                ErrorMessage: "Model overloaded"));
        _providerFactory.GetChatProvider(Arg.Any<string>()).Returns(provider);

        var result = await CreateHandler().Handle(
            new SendAssistantMessage.Command(
                ConversationId: null,
                Message: "Any service degraded?",
                ContextScope: "services",
                Persona: "Engineer",
                PreferredModelId: null,
                ClientType: "Web",
                ServiceId: null, ContractId: null, IncidentId: null,
                ChangeId: null, TeamId: null, DomainId: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDegraded.Should().BeTrue();
    }

    // ── Test 8: grounding sources included in response ───────────────────

    [Fact]
    public async Task Handle_WhenProviderAvailable_IncludesGroundingSourcesInResponse()
    {
        MockProvider("Grounded response.");

        var result = await CreateHandler().Handle(
            new SendAssistantMessage.Command(
                ConversationId: null,
                Message: "List active runbooks.",
                ContextScope: "operations",
                Persona: "Engineer",
                PreferredModelId: null,
                ClientType: "Web",
                ServiceId: null, ContractId: null, IncidentId: null,
                ChangeId: null, TeamId: null, DomainId: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.GroundingSources.Should().NotBeNull();
    }

    // ── Test 9: quota exceeded returns error before inference ─────────────

    [Fact]
    public async Task Handle_WhenQuotaExceeded_ReturnsQuotaExceededError()
    {
        // Override quota service to deny
        _quotaService.ValidateQuotaAsync(
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new TokenQuotaValidationResult(IsAllowed: false, BlockReason: "Daily limit exceeded"));

        var result = await CreateHandler().Handle(
            new SendAssistantMessage.Command(
                ConversationId: null,
                Message: "What is the health of service X?",
                ContextScope: null,
                Persona: "Engineer",
                PreferredModelId: null,
                ClientType: "Web",
                ServiceId: null, ContractId: null, IncidentId: null,
                ChangeId: null, TeamId: null, DomainId: null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("QuotaExceeded");

        // Provider should NOT have been called
        _providerFactory.DidNotReceive().GetChatProvider(Arg.Any<string>());
    }
}
