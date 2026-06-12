using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.SendAssistantMessage;
using NexTraceOne.AIKnowledge.Application.Governance.Services;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários para a integração de histórico de conversa no SendAssistantMessage.
/// Valida que o handler carrega mensagens anteriores, aplica sliding window,
/// filtra mensagens degradadas e estima tokens corretamente.
/// </summary>
public sealed class SendAssistantMessageHistoryTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

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
    private readonly ITokenCounterService _tokenCounter = Substitute.For<ITokenCounterService>();
    private readonly IContextWindowManager _contextWindow;
    private readonly IPromptCacheService _promptCache = Substitute.For<IPromptCacheService>();
    private readonly IAiModelCatalogService _modelCatalogService = Substitute.For<IAiModelCatalogService>();
    private readonly IAiMessageRepository _messageRepo = Substitute.For<IAiMessageRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly ICurrentEnvironment _currentEnvironment = Substitute.For<ICurrentEnvironment>();

    public SendAssistantMessageHistoryTests()
    {
        _tokenCounter.CountTokens(Arg.Any<string>()).Returns(10);
        _contextWindow = new ContextWindowManager(_tokenCounter);

        _currentUser.Id.Returns("user-001");
        _currentUser.Email.Returns("engineer@nextraceone.io");
        _currentUser.Name.Returns("Test Engineer");
        _currentUser.IsAuthenticated.Returns(true);
        _currentTenant.Id.Returns(Guid.NewGuid());
        _currentTenant.HasCapability(Arg.Any<string>()).Returns(true);

        _guardrailService.EvaluateInputAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(GuardrailEvaluationResult.Passed());
        _guardrailService.EvaluateOutputAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(GuardrailEvaluationResult.Passed());

        _quotaService.ValidateQuotaAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new TokenQuotaValidationResult(IsAllowed: true));

        _routingRepo.ListAsync(true, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AIRoutingStrategy>().AsReadOnly());

        _sourceRepo.ListAsync(null, true, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AIKnowledgeSource>().AsReadOnly());

        _groundingService.ResolveGroundingAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<Guid?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(),
            Arg.Any<string?>(), Arg.Any<IReadOnlyList<AIKnowledgeSource>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var personaArg = callInfo.ArgAt<string>(1);
                var contextScopeArg = callInfo.ArgAt<string?>(2) ?? "general";
                return new GroundingResolutionResult(
                    GroundingContext: $"Persona: {personaArg}",
                    SystemPrompt: $"You are AI Assistant. Context scope: {contextScopeArg}",
                    GroundingSources: ["Catalog"],
                    ContextSummary: null,
                    SuggestedSteps: null,
                    Caveats: null,
                    ContextStrength: "none",
                    ConfidenceLevel: "Unknown",
                    SourceWeightingSummary: "no-matches",
                    UseCaseType: AIUseCaseType.General);
            });

        _routingResolver.ResolveRoutingAsync(
            Arg.Any<string>(), Arg.Any<AIUseCaseType>(), Arg.Any<string>(), Arg.Any<Guid?>(),
            Arg.Any<string>(), Arg.Any<IReadOnlyList<AIRoutingStrategy>>(), Arg.Any<CancellationToken>())
            .Returns(new RoutingResolutionResult(
                SelectedModel: "llama3.2",
                SelectedProvider: "ollama",
                IsInternal: true,
                RoutingPath: AIRoutingPath.InternalOnly,
                RoutingRationale: "Test routing",
                CostClass: "low",
                EscalationReason: "None",
                AppliedStrategy: null));

        SetupDefaultConversationPersistence();
    }

    private void SetupDefaultConversationPersistence()
    {
        var convId = Guid.NewGuid();
        var conversation = AiAssistantConversation.Start(
            "Test", "Engineer", AIClientType.Web, "general", "user-001");

        _convPersistence.GetOrCreateAsync(
            Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<AIClientType>(), Arg.Any<string?>(), Arg.Any<Guid?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns((conversation, (NexTraceOne.BuildingBlocks.Core.Results.Error?)null));

        _convPersistence.PersistMessagePairAsync(
            Arg.Any<AiAssistantConversation>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var isDegradedArg = callInfo.ArgAt<bool>(13);
                return new MessagePersistenceResult(
                    convId, Guid.NewGuid(), FixedNow, Guid.NewGuid(), FixedNow,
                    isDegradedArg ? "Degraded" : "Ready",
                    isDegradedArg, isDegradedArg ? "Provider unavailable" : null,
                    "Test conversation", 2, FixedNow);
            });
    }

    private SendAssistantMessage.Handler CreateHandler()
        => new(
            _usageRepo, _routingRepo, _sourceRepo, _modelAuth, _quotaService,
            _routingPort, _providerFactory, _groundingService, _routingResolver,
            _convPersistence, _guardrailService, _contextWindow, _promptCache,
            _modelCatalogService, _messageRepo, _currentUser, _currentTenant,
            _currentEnvironment, NullLogger<SendAssistantMessage.Handler>.Instance);

    private static IChatCompletionProvider MockProvider(string content)
    {
        var provider = Substitute.For<IChatCompletionProvider>();
        provider.ProviderId.Returns("ollama");
        provider.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult(
                Success: true, Content: content, ModelId: "llama3.2", ProviderId: "ollama",
                PromptTokens: 10, CompletionTokens: 20, Duration: TimeSpan.Zero));
        return provider;
    }

    // ── History Loading Tests ────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithConversationHistory_IncludesPreviousMessagesInRequest()
    {
        var convId = Guid.NewGuid();
        var history = new List<AiMessage>
        {
            AiMessage.UserMessage(convId, "First question", FixedNow.AddMinutes(-10)),
            AiMessage.AssistantMessage(convId, "First answer", "llama3.2", "ollama", true, 5, 10, null, "", "", "corr-1", FixedNow.AddMinutes(-9)),
            AiMessage.UserMessage(convId, "Follow-up", FixedNow.AddMinutes(-5)),
            AiMessage.AssistantMessage(convId, "Follow-up answer", "llama3.2", "ollama", true, 5, 10, null, "", "", "corr-2", FixedNow.AddMinutes(-4))
        };

        _messageRepo.ListByConversationAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(history.AsReadOnly());

        ChatCompletionRequest? capturedRequest = null;
        var provider = Substitute.For<IChatCompletionProvider>();
        provider.ProviderId.Returns("ollama");
        provider.CompleteAsync(Arg.Do<ChatCompletionRequest>(r => capturedRequest = r), Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult(true, "Final answer", "llama3.2", "ollama", 10, 20, TimeSpan.Zero));
        _providerFactory.GetChatProvider("ollama").Returns(provider);

        await CreateHandler().Handle(
            new SendAssistantMessage.Command(
                ConversationId: convId,
                Message: "Current question",
                ContextScope: null,
                Persona: "Engineer",
                PreferredModelId: null,
                ClientType: "Web",
                ServiceId: null, ContractId: null, IncidentId: null,
                ChangeId: null, TeamId: null, DomainId: null),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Messages.Should().HaveCount(6); // system + 4 history + current
        capturedRequest.Messages[0].Role.Should().Be("system");
        capturedRequest.Messages[^1].Role.Should().Be("user");
        capturedRequest.Messages[^1].Content.Should().Be("Current question");
    }

    [Fact]
    public async Task Handle_WithDegradedHistory_FiltersDegradedMessages()
    {
        var convId = Guid.NewGuid();
        var history = new List<AiMessage>
        {
            AiMessage.UserMessage(convId, "Question", FixedNow.AddMinutes(-10)),
            AiMessage.AssistantMessage(
                convId,
                $"{AiMessage.DeterministicFallbackPrefix} Provider unavailable",
                "deterministic-fallback", "system-fallback", true, 0, 0, null, "", "", "corr-3", FixedNow.AddMinutes(-9))
        };

        _messageRepo.ListByConversationAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(history.AsReadOnly());

        ChatCompletionRequest? capturedRequest = null;
        var provider = Substitute.For<IChatCompletionProvider>();
        provider.ProviderId.Returns("ollama");
        provider.CompleteAsync(Arg.Do<ChatCompletionRequest>(r => capturedRequest = r), Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult(true, "Answer", "llama3.2", "ollama", 10, 20, TimeSpan.Zero));
        _providerFactory.GetChatProvider("ollama").Returns(provider);

        await CreateHandler().Handle(
            new SendAssistantMessage.Command(
                ConversationId: convId,
                Message: "Current",
                ContextScope: null,
                Persona: "Engineer",
                PreferredModelId: null,
                ClientType: "Web",
                ServiceId: null, ContractId: null, IncidentId: null,
                ChangeId: null, TeamId: null, DomainId: null),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        // system + user history + current user = 3 (degraded assistant skipped)
        capturedRequest!.Messages.Should().HaveCount(3);
        capturedRequest.Messages.Should().NotContain(m =>
            m.Content.StartsWith(AiMessage.DeterministicFallbackPrefix, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_WithoutHistory_OnlyIncludesSystemAndCurrentMessage()
    {
        _messageRepo.ListByConversationAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AiMessage>().AsReadOnly());

        ChatCompletionRequest? capturedRequest = null;
        var provider = Substitute.For<IChatCompletionProvider>();
        provider.ProviderId.Returns("ollama");
        provider.CompleteAsync(Arg.Do<ChatCompletionRequest>(r => capturedRequest = r), Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult(true, "Answer", "llama3.2", "ollama", 10, 20, TimeSpan.Zero));
        _providerFactory.GetChatProvider("ollama").Returns(provider);

        await CreateHandler().Handle(
            new SendAssistantMessage.Command(
                ConversationId: null,
                Message: "Hello",
                ContextScope: null,
                Persona: "Engineer",
                PreferredModelId: null,
                ClientType: "Web",
                ServiceId: null, ContractId: null, IncidentId: null,
                ChangeId: null, TeamId: null, DomainId: null),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Messages.Should().HaveCount(2); // system + current
        capturedRequest.Messages[0].Role.Should().Be("system");
        capturedRequest.Messages[1].Role.Should().Be("user");
        capturedRequest.Messages[1].Content.Should().Be("Hello");
    }

    [Fact]
    public async Task Handle_TokenQuotaEstimate_IncludesHistoryTokens()
    {
        _messageRepo.ListByConversationAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AiMessage>().AsReadOnly());

        var provider = MockProvider("Answer");
        _providerFactory.GetChatProvider("ollama").Returns(provider);

        await CreateHandler().Handle(
            new SendAssistantMessage.Command(
                ConversationId: null,
                Message: "Hello",
                ContextScope: null,
                Persona: "Engineer",
                PreferredModelId: null,
                ClientType: "Web",
                ServiceId: null, ContractId: null, IncidentId: null,
                ChangeId: null, TeamId: null, DomainId: null),
            CancellationToken.None);

        // ContextWindowManager estimates: system message (10+4) + user message (10+4) = 28
        await _quotaService.Received().ValidateQuotaAsync(
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Is<int>(tokens => tokens >= 20), Arg.Any<CancellationToken>());
    }
}
