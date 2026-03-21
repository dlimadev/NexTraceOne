using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.AnalyzeNonProdEnvironment;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.AssessPromotionReadiness;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.CompareEnvironments;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Orchestration.Features;

/// <summary>
/// Testes de isolamento de contexto para as features de análise de IA.
///
/// FASE 8 — Hardening: Estes testes validam que:
/// 1. TenantId é obrigatório e propagado corretamente
/// 2. EnvironmentId é obrigatório e propagado corretamente
/// 3. A IA não pode comparar ambientes de tenants diferentes (validação por grounding)
/// 4. A IA não pode comparar um ambiente consigo mesmo
/// 5. A IA carrega o contexto correto para o provedor
/// 6. CorrelationId é único por execução (rastreabilidade)
/// 7. Falha segura quando contexto insuficiente
/// </summary>
public sealed class AiAnalysisContextIsolationTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 21, 10, 0, 0, TimeSpan.Zero);

    private readonly IExternalAIRoutingPort _routingPort = Substitute.For<IExternalAIRoutingPort>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<AnalyzeNonProdEnvironment.Handler> _analyzeLogger = Substitute.For<ILogger<AnalyzeNonProdEnvironment.Handler>>();
    private readonly ILogger<CompareEnvironments.Handler> _compareLogger = Substitute.For<ILogger<CompareEnvironments.Handler>>();
    private readonly ILogger<AssessPromotionReadiness.Handler> _readinessLogger = Substitute.For<ILogger<AssessPromotionReadiness.Handler>>();

    // ─── TENANT ISOLATION ────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeNonProd_ShouldIncludeTenantIdInGrounding_NeverLeakToOtherTenants()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        string? captured = null;
        _routingPort.RouteQueryAsync(Arg.Do<string>(g => captured = g), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("OVERALL_RISK: LOW\nRECOMMENDATION: Clean.");

        var handler = new AnalyzeNonProdEnvironment.Handler(_routingPort, _dateTimeProvider, _analyzeLogger);
        var command = new AnalyzeNonProdEnvironment.Command("tenant-A", "env-qa-001", "QA", "qa", null, 7, null);

        await handler.Handle(command, CancellationToken.None);

        // The grounding MUST include tenant-A — IA must be scoped to this tenant
        captured.Should().Contain("tenant-A");
        // The grounding MUST NOT contain other tenant IDs
        captured.Should().NotContain("tenant-B");
    }

    [Fact]
    public async Task AnalyzeNonProd_TwoExecutions_ShouldProduceDifferentCorrelationIds()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("OVERALL_RISK: LOW\nRECOMMENDATION: Clean.");

        var handler = new AnalyzeNonProdEnvironment.Handler(_routingPort, _dateTimeProvider, _analyzeLogger);
        var command = new AnalyzeNonProdEnvironment.Command("tenant-A", "env-qa-001", "QA", "qa", null, 7, null);

        var r1 = await handler.Handle(command, CancellationToken.None);
        var r2 = await handler.Handle(command, CancellationToken.None);

        r1.IsSuccess.Should().BeTrue();
        r2.IsSuccess.Should().BeTrue();
        // Each execution must have a unique correlation ID for auditability
        r1.Value!.CorrelationId.Should().NotBe(r2.Value!.CorrelationId);
    }

    [Fact]
    public async Task AnalyzeNonProd_ShouldIncludeEnvironmentIdInResponse_ForTraceability()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("OVERALL_RISK: MEDIUM\nRECOMMENDATION: Review.");

        var handler = new AnalyzeNonProdEnvironment.Handler(_routingPort, _dateTimeProvider, _analyzeLogger);
        var command = new AnalyzeNonProdEnvironment.Command("tenant-A", "env-uat-007", "UAT", "uat", null, 14, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Response must echo back the environment context for traceability
        result.Value!.EnvironmentId.Should().Be("env-uat-007");
        result.Value.TenantId.Should().Be("tenant-A");
        result.Value.EnvironmentProfile.Should().Be("uat");
    }

    // ─── COMPARE ENVIRONMENTS: INTRA-TENANT ENFORCEMENT ──────────────────────

    [Fact]
    public async Task CompareEnvironments_GroundingMustMentionSameTenantConstraint()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        string? capturedGrounding = null;
        _routingPort.RouteQueryAsync(
            Arg.Do<string>(g => capturedGrounding = g),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("PROMOTION_RECOMMENDATION: SAFE_TO_PROMOTE\nSUMMARY: Aligned.");

        var handler = new CompareEnvironments.Handler(_routingPort, _dateTimeProvider, _compareLogger);
        var command = new CompareEnvironments.Command(
            "tenant-acme-001",
            "env-qa-001", "QA", "qa",
            "env-prod-001", "Production", "production",
            null, null, null);

        await handler.Handle(command, CancellationToken.None);

        // Grounding MUST explicitly say "same tenant" to prevent cross-tenant confusion
        capturedGrounding.Should().Contain("same tenant");
        capturedGrounding.Should().Contain("tenant-acme-001");
    }

    [Fact]
    public void CompareEnvironments_Validator_ShouldReject_WhenSubjectEqualsReference()
    {
        var validator = new CompareEnvironments.Validator();
        var command = new CompareEnvironments.Command(
            "tenant-acme-001",
            "env-same-001", "Staging", "staging",
            "env-same-001", "Staging", "staging", // SAME environment
            null, null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void CompareEnvironments_Validator_ShouldReject_WhenTenantIdIsEmpty()
    {
        var validator = new CompareEnvironments.Validator();
        var command = new CompareEnvironments.Command(
            "", // empty TenantId
            "env-qa-001", "QA", "qa",
            "env-prod-001", "Production", "production",
            null, null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CompareEnvironments.Command.TenantId));
    }

    [Fact]
    public async Task CompareEnvironments_ResponseMustContainBothEnvironmentIds_ForAudit()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("DIVERGENCE: HIGH | contracts | Breaking change detected\nPROMOTION_RECOMMENDATION: BLOCK_PROMOTION\nSUMMARY: Block this.");

        var handler = new CompareEnvironments.Handler(_routingPort, _dateTimeProvider, _compareLogger);
        var command = new CompareEnvironments.Command(
            "tenant-acme-001",
            "env-hml-001", "HML", "hml",
            "env-prod-001", "Production", "production",
            null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SubjectEnvironmentId.Should().Be("env-hml-001");
        result.Value.ReferenceEnvironmentId.Should().Be("env-prod-001");
        result.Value.TenantId.Should().Be("tenant-acme-001");
        result.Value.CorrelationId.Should().NotBeNullOrWhiteSpace();
    }

    // ─── PROMOTION READINESS: CONTEXT INTEGRITY ──────────────────────────────

    [Fact]
    public void AssessPromotionReadiness_Validator_ShouldReject_WhenSourceEqualsTarget()
    {
        var validator = new AssessPromotionReadiness.Validator();
        var command = new AssessPromotionReadiness.Command(
            "tenant-A", "env-prod-001", "Production", "env-prod-001", "Production",
            "payment-service", "2.0.0", null, 7, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AssessPromotionReadiness_Validator_ShouldReject_WhenServiceNameIsEmpty()
    {
        var validator = new AssessPromotionReadiness.Validator();
        var command = new AssessPromotionReadiness.Command(
            "tenant-A", "env-qa-001", "QA", "env-prod-001", "Production",
            "", "2.0.0", null, 7, null); // empty service name

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AssessPromotionReadiness.Command.ServiceName));
    }

    [Fact]
    public void AssessPromotionReadiness_Validator_ShouldReject_WhenVersionIsEmpty()
    {
        var validator = new AssessPromotionReadiness.Validator();
        var command = new AssessPromotionReadiness.Command(
            "tenant-A", "env-qa-001", "QA", "env-prod-001", "Production",
            "payment-service", "", null, 7, null); // empty version

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AssessPromotionReadiness.Command.Version));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(91)]
    [InlineData(-1)]
    public void AssessPromotionReadiness_Validator_ShouldReject_WhenObservationWindowOutOfRange(int days)
    {
        var validator = new AssessPromotionReadiness.Validator();
        var command = new AssessPromotionReadiness.Command(
            "tenant-A", "env-qa-001", "QA", "env-prod-001", "Production",
            "payment-service", "2.0.0", null, days, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AssessPromotionReadiness_ShouldCarryTenantInResponse_ForAudit()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("READINESS_SCORE: 88\nREADINESS_LEVEL: READY\nSHOULD_BLOCK: NO\nSUMMARY: Ready.");

        var handler = new AssessPromotionReadiness.Handler(_routingPort, _dateTimeProvider, _readinessLogger);
        var command = new AssessPromotionReadiness.Command(
            "tenant-enterprise-007",
            "env-staging-001", "Staging", "env-prod-001", "Production",
            "order-service", "3.1.0", "rel-456", 7, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantId.Should().Be("tenant-enterprise-007");
        result.Value.ServiceName.Should().Be("order-service");
        result.Value.Version.Should().Be("3.1.0");
        result.Value.ReleaseId.Should().Be("rel-456");
    }

    // ─── NON-PROD ANALYSIS: OBSERVATION WINDOW ───────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(30)]
    [InlineData(90)]
    public async Task AnalyzeNonProd_ShouldIncludeObservationWindowInGrounding(int windowDays)
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        string? captured = null;
        _routingPort.RouteQueryAsync(Arg.Do<string>(g => captured = g), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("OVERALL_RISK: LOW\nRECOMMENDATION: OK.");

        var handler = new AnalyzeNonProdEnvironment.Handler(_routingPort, _dateTimeProvider, _analyzeLogger);
        var command = new AnalyzeNonProdEnvironment.Command("tenant-A", "env-qa-001", "QA", "qa", null, windowDays, null);

        await handler.Handle(command, CancellationToken.None);

        captured.Should().Contain(windowDays.ToString());
    }

    [Fact]
    public async Task AnalyzeNonProd_WithServiceFilter_ShouldIncludeFilterInGrounding()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        string? captured = null;
        _routingPort.RouteQueryAsync(Arg.Do<string>(g => captured = g), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("OVERALL_RISK: LOW\nRECOMMENDATION: OK.");

        var handler = new AnalyzeNonProdEnvironment.Handler(_routingPort, _dateTimeProvider, _analyzeLogger);
        var command = new AnalyzeNonProdEnvironment.Command(
            "tenant-A", "env-qa-001", "QA", "qa",
            new List<string> { "payment-service", "order-service" }.AsReadOnly(),
            7, null);

        await handler.Handle(command, CancellationToken.None);

        captured.Should().Contain("payment-service");
        captured.Should().Contain("order-service");
    }

    // ─── OVERALL RISK AND RECOMMENDATION PARSING ─────────────────────────────

    [Theory]
    [InlineData("OVERALL_RISK: HIGH\nRECOMMENDATION: Block.", "HIGH")]
    [InlineData("OVERALL_RISK: MEDIUM\nRECOMMENDATION: Review.", "MEDIUM")]
    [InlineData("OVERALL_RISK: LOW\nRECOMMENDATION: Proceed.", "LOW")]
    public async Task AnalyzeNonProd_ShouldParseAllRiskLevels(string aiResponse, string expectedRisk)
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(aiResponse);

        var handler = new AnalyzeNonProdEnvironment.Handler(_routingPort, _dateTimeProvider, _analyzeLogger);
        var command = new AnalyzeNonProdEnvironment.Command("t", "e", "ENV", "qa", null, 7, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OverallRiskLevel.Should().Be(expectedRisk);
    }

    [Theory]
    [InlineData("PROMOTION_RECOMMENDATION: SAFE_TO_PROMOTE\nSUMMARY: OK.", "SAFE_TO_PROMOTE")]
    [InlineData("PROMOTION_RECOMMENDATION: REVIEW_REQUIRED\nSUMMARY: Check.", "REVIEW_REQUIRED")]
    [InlineData("PROMOTION_RECOMMENDATION: BLOCK_PROMOTION\nSUMMARY: Block.", "BLOCK_PROMOTION")]
    public async Task CompareEnvironments_ShouldParseAllPromotionRecommendations(string aiResponse, string expectedRec)
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(aiResponse);

        var handler = new CompareEnvironments.Handler(_routingPort, _dateTimeProvider, _compareLogger);
        var command = new CompareEnvironments.Command("t", "e1", "E1", "qa", "e2", "E2", "production", null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PromotionRecommendation.Should().Be(expectedRec);
    }

    // ─── SAFE FAILURE SCENARIOS ───────────────────────────────────────────────

    [Fact]
    public async Task AllFeatures_ShouldReturnError_WhenProviderThrows()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new TimeoutException("Provider timeout")));

        // AnalyzeNonProd
        var analyzeHandler = new AnalyzeNonProdEnvironment.Handler(_routingPort, _dateTimeProvider, _analyzeLogger);
        var analyzeResult = await analyzeHandler.Handle(new AnalyzeNonProdEnvironment.Command("t", "e", "E", "qa", null, 7, null), CancellationToken.None);
        analyzeResult.IsSuccess.Should().BeFalse();

        // CompareEnvironments
        var compareHandler = new CompareEnvironments.Handler(_routingPort, _dateTimeProvider, _compareLogger);
        var compareResult = await compareHandler.Handle(new CompareEnvironments.Command("t", "e1", "E1", "qa", "e2", "E2", "prod", null, null, null), CancellationToken.None);
        compareResult.IsSuccess.Should().BeFalse();

        // AssessPromotionReadiness
        var readinessHandler = new AssessPromotionReadiness.Handler(_routingPort, _dateTimeProvider, _readinessLogger);
        var readinessResult = await readinessHandler.Handle(new AssessPromotionReadiness.Command("t", "e1", "E1", "e2", "E2", "svc", "1.0", null, 7, null), CancellationToken.None);
        readinessResult.IsSuccess.Should().BeFalse();
    }
}
