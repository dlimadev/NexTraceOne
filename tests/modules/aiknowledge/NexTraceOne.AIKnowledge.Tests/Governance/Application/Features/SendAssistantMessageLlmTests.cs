using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.SendAssistantMessage;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
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
    private readonly IAiAssistantConversationRepository _convRepo = Substitute.For<IAiAssistantConversationRepository>();
    private readonly IAiMessageRepository _msgRepo = Substitute.For<IAiMessageRepository>();
    private readonly IAiRoutingStrategyRepository _routingRepo = Substitute.For<IAiRoutingStrategyRepository>();
    private readonly IAiKnowledgeSourceRepository _sourceRepo = Substitute.For<IAiKnowledgeSourceRepository>();
    private readonly IAiModelCatalogService _modelCatalog = Substitute.For<IAiModelCatalogService>();
    private readonly IAiModelAuthorizationService _modelAuth = Substitute.For<IAiModelAuthorizationService>();
    private readonly IExternalAIRoutingPort _routingPort = Substitute.For<IExternalAIRoutingPort>();
    private readonly IAiProviderFactory _providerFactory = Substitute.For<IAiProviderFactory>();
    private readonly IDocumentRetrievalService _docRetrieval = Substitute.For<IDocumentRetrievalService>();
    private readonly IDatabaseRetrievalService _dbRetrieval = Substitute.For<IDatabaseRetrievalService>();
    private readonly ITelemetryRetrievalService _telRetrieval = Substitute.For<ITelemetryRetrievalService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ICurrentEnvironment _currentEnvironment = Substitute.For<ICurrentEnvironment>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public SendAssistantMessageLlmTests()
    {
        // Default user setup
        _currentUser.Id.Returns("user-001");
        _currentUser.Email.Returns("engineer@nextraceone.io");
        _currentUser.Name.Returns("Test Engineer");
        _currentUser.IsAuthenticated.Returns(true);
        _clock.UtcNow.Returns(FixedNow);

        // Default repository stubs
        _routingRepo.ListAsync(true, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NexTraceOne.AIKnowledge.Domain.Governance.Entities.AIRoutingStrategy>().AsReadOnly());
        _sourceRepo.ListAsync(null, true, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NexTraceOne.AIKnowledge.Domain.Governance.Entities.AIKnowledgeSource>().AsReadOnly());
        _modelCatalog.ResolveDefaultModelAsync("chat", Arg.Any<CancellationToken>())
            .Returns((ResolvedModel?)null);

        // Retrieval services return empty by default
        _docRetrieval.SearchAsync(Arg.Any<DocumentSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DocumentSearchResult(false, []));
        _dbRetrieval.SearchAsync(Arg.Any<DatabaseSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DatabaseSearchResult(false, []));
        _telRetrieval.SearchAsync(Arg.Any<TelemetrySearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TelemetrySearchResult(false, []));
    }

    private SendAssistantMessage.Handler CreateHandler() => new(
        _usageRepo, _convRepo, _msgRepo, _routingRepo, _sourceRepo,
        _modelCatalog, _modelAuth,
        _routingPort, _providerFactory,
        _docRetrieval, _dbRetrieval, _telRetrieval,
        _currentUser, _currentEnvironment, _clock,
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
}
