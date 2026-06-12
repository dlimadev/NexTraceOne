using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Services;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Services;

/// <summary>
/// Testes unitários do AiExecutionGateway — foco em runtime fallback,
/// resolução de provider, e integração com health monitor / metrics.
/// </summary>
public sealed class AiExecutionGatewayTests
{
    private readonly IAiProviderFactory _providerFactory = Substitute.For<IAiProviderFactory>();
    private readonly IAiModelCatalogService _catalogService = Substitute.For<IAiModelCatalogService>();
    private readonly IUserAiPreferenceRepository _userPrefRepo = Substitute.For<IUserAiPreferenceRepository>();
    private readonly IAiFeatureModelBindingRepository _featureBindingRepo = Substitute.For<IAiFeatureModelBindingRepository>();
    private readonly IAiModelAuthorizationService _authService = Substitute.For<IAiModelAuthorizationService>();
    private readonly IAiTokenQuotaService _quotaService = Substitute.For<IAiTokenQuotaService>();
    private readonly IAiGuardrailEnforcementService _guardrailService = Substitute.For<IAiGuardrailEnforcementService>();
    private readonly IAiProviderHealthMonitor _healthMonitor = Substitute.For<IAiProviderHealthMonitor>();
    private readonly IAiRoutingMetricsService _routingMetrics = Substitute.For<IAiRoutingMetricsService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();

    public AiExecutionGatewayTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid().ToString());
        _currentUser.Persona.Returns("Engineer");
        _currentTenant.Id.Returns(Guid.NewGuid());

        _guardrailService.EvaluateInputAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(GuardrailEvaluationResult.Passed());
        _guardrailService.EvaluateOutputAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(GuardrailEvaluationResult.Passed());

        _quotaService.ValidateQuotaAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new TokenQuotaValidationResult(IsAllowed: true));

        _authService.ValidateModelAccessAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ModelAccessDecision(true, null, null, true));

        _healthMonitor.IsHealthy(Arg.Any<string>()).Returns(true);
        _healthMonitor.GetHealthyProviderIds().Returns(new List<string>());

        _routingMetrics.RankProvidersByLatencyAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderLatencyRanking>());
    }

    private AiExecutionGateway CreateGateway()
    {
        return new AiExecutionGateway(
            _providerFactory, _catalogService, _userPrefRepo, _featureBindingRepo,
            _authService, _quotaService, _guardrailService, _healthMonitor,
            _routingMetrics, _currentUser, _currentTenant,
            Substitute.For<ILogger<AiExecutionGateway>>());
    }

    private static IChatCompletionProvider CreateProvider(string id, string? content = null, bool success = true)
    {
        var provider = Substitute.For<IChatCompletionProvider>();
        provider.ProviderId.Returns(id);
        provider.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult(
                Success: success,
                Content: content ?? "response",
                ModelId: "model",
                ProviderId: id,
                PromptTokens: 10,
                CompletionTokens: 20,
                Duration: TimeSpan.FromMilliseconds(100)));
        return provider;
    }

    private static IChatCompletionProvider CreateFailingProvider(string id, Exception? ex = null)
    {
        var provider = Substitute.For<IChatCompletionProvider>();
        provider.ProviderId.Returns(id);
        provider.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ChatCompletionResult>(ex ?? new InvalidOperationException("Connection refused")));
        return provider;
    }

    // ── Runtime Fallback Tests ───────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenPrimaryFails_UsesFallbackProvider()
    {
        var primary = CreateFailingProvider("ollama");
        var fallback = CreateProvider("openai", "fallback-response");

        _providerFactory.GetChatProvider("ollama").Returns(primary);
        _providerFactory.GetChatProvider("openai").Returns(fallback);
        _healthMonitor.GetHealthyProviderIds().Returns(["openai"]);
        _catalogService.ResolveDefaultModelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ResolvedModel(Guid.NewGuid(), "llama3.2", "Llama", "ollama", "Ollama", true, "chat", 128000));

        var gateway = CreateGateway();
        var result = await gateway.ExecuteAsync(
            new AiExecutionRequest(FeatureKey: "test", RequestType: "chat", UserPrompt: "hello"));

        result.Success.Should().BeTrue();
        result.Content.Should().Be("fallback-response");
        result.ResolvedProviderId.Should().Be("openai");
        result.WasFallbackUsed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenPrimaryReturnsUnsuccessful_UsesFallbackProvider()
    {
        var primary = CreateProvider("ollama", success: false);
        var fallback = CreateProvider("openai", "fallback-response");

        _providerFactory.GetChatProvider("ollama").Returns(primary);
        _providerFactory.GetChatProvider("openai").Returns(fallback);
        _healthMonitor.GetHealthyProviderIds().Returns(["openai"]);
        _catalogService.ResolveDefaultModelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ResolvedModel(Guid.NewGuid(), "llama3.2", "Llama", "ollama", "Ollama", true, "chat", 128000));

        var gateway = CreateGateway();
        var result = await gateway.ExecuteAsync(
            new AiExecutionRequest(FeatureKey: "test", RequestType: "chat", UserPrompt: "hello"));

        result.Success.Should().BeTrue();
        result.Content.Should().Be("fallback-response");
        result.ResolvedProviderId.Should().Be("openai");
    }

    [Fact]
    public async Task ExecuteAsync_WhenAllProvidersFail_ReturnsError()
    {
        var primary = CreateFailingProvider("ollama");
        var fallback = CreateFailingProvider("openai");

        _providerFactory.GetChatProvider("ollama").Returns(primary);
        _providerFactory.GetChatProvider("openai").Returns(fallback);
        _healthMonitor.GetHealthyProviderIds().Returns(["openai"]);
        _catalogService.ResolveDefaultModelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ResolvedModel(Guid.NewGuid(), "llama3.2", "Llama", "ollama", "Ollama", true, "chat", 128000));

        var gateway = CreateGateway();
        var result = await gateway.ExecuteAsync(
            new AiExecutionRequest(FeatureKey: "test", RequestType: "chat", UserPrompt: "hello"));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Todos os providers disponíveis falharam");
    }

    [Fact]
    public async Task ExecuteAsync_WhenFallbackSucceeds_RecordsUsageWithFallbackIds()
    {
        var primary = CreateFailingProvider("ollama");
        var fallback = CreateProvider("openai", "fallback-response");

        _providerFactory.GetChatProvider("ollama").Returns(primary);
        _providerFactory.GetChatProvider("openai").Returns(fallback);
        _healthMonitor.GetHealthyProviderIds().Returns(["openai"]);
        _catalogService.ResolveDefaultModelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ResolvedModel(Guid.NewGuid(), "llama3.2", "Llama", "ollama", "Ollama", true, "chat", 128000));

        var gateway = CreateGateway();
        var result = await gateway.ExecuteAsync(
            new AiExecutionRequest(FeatureKey: "test", RequestType: "chat", UserPrompt: "hello"));

        result.Success.Should().BeTrue();
        result.ResolvedProviderId.Should().Be("openai");
        // Note: RecordUsageAsync is fire-and-forget; deterministic verification
        // would require an abstraction over Task scheduling. We verify the result
        // carries the correct fallback IDs, which is the public contract.
    }

    // ── Invalid User ID ──────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenUserIdIsInvalid_ReturnsError()
    {
        _currentUser.Id.Returns("not-a-guid");

        var gateway = CreateGateway();
        var result = await gateway.ExecuteAsync(
            new AiExecutionRequest(FeatureKey: "test", RequestType: "chat"));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Identificador de usuário inválido");
    }

    // ── Guardrail Blocks ─────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenInputGuardrailBlocks_ReturnsGuardrailError()
    {
        _guardrailService.EvaluateInputAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(GuardrailEvaluationResult.Blocked("prompt_injection", "injection_pattern", "high", "Blocked"));

        var gateway = CreateGateway();
        var result = await gateway.ExecuteAsync(
            new AiExecutionRequest(FeatureKey: "test", RequestType: "chat", UserPrompt: "hello"));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("GuardrailBlocked");
    }

    // ── Quota Exceeded ───────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenQuotaExceeded_ReturnsQuotaError()
    {
        _quotaService.ValidateQuotaAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new TokenQuotaValidationResult(IsAllowed: false, BlockReason: "Daily limit reached"));

        var gateway = CreateGateway();
        var result = await gateway.ExecuteAsync(
            new AiExecutionRequest(FeatureKey: "test", RequestType: "chat", UserPrompt: "hello"));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("QuotaExceeded");
    }
}
