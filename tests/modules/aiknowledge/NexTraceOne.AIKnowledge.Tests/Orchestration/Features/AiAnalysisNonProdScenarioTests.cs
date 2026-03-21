using System.Linq;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.AnalyzeNonProdEnvironment;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.AssessPromotionReadiness;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.CompareEnvironments;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Orchestration.Features;

/// <summary>
/// Testes de cenários de ambientes não produtivos para as features de análise de IA.
///
/// FASE 8 — Validação da IA para prevenção de problemas em produção.
/// Estes testes validam os cenários prioritários do produto:
/// - Análise de DEV/QA/UAT/HML/STAGING
/// - Detecção de regressões antes do go-live
/// - Bloqueio de promoções arriscadas
/// - Readiness para produção
/// - Comparação entre ambientes
/// </summary>
public sealed class AiAnalysisNonProdScenarioTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 21, 10, 0, 0, TimeSpan.Zero);
    private readonly IExternalAIRoutingPort _routingPort = Substitute.For<IExternalAIRoutingPort>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<AnalyzeNonProdEnvironment.Handler> _analyzeLogger = Substitute.For<ILogger<AnalyzeNonProdEnvironment.Handler>>();
    private readonly ILogger<CompareEnvironments.Handler> _compareLogger = Substitute.For<ILogger<CompareEnvironments.Handler>>();
    private readonly ILogger<AssessPromotionReadiness.Handler> _readinessLogger = Substitute.For<ILogger<AssessPromotionReadiness.Handler>>();

    // ─── SCENARIO: QA RISK ANALYSIS ──────────────────────────────────────────

    [Fact]
    public async Task Scenario_QARiskAnalysis_ShouldDetectHighRiskWithContractDrift()
    {
        // SCENARIO: A QA environment has a service with a breaking contract change.
        // The AI should detect this and flag it as HIGH risk to prevent it reaching production.
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(
                "FINDING: HIGH | contract-drift | payment-service v2.1 has a breaking change in /payments response schema\n" +
                "FINDING: MEDIUM | error-rate | order-service error rate is 12% above baseline\n" +
                "OVERALL_RISK: HIGH\n" +
                "RECOMMENDATION: Block promotion. Resolve contract drift in payment-service before go-live.");

        var handler = new AnalyzeNonProdEnvironment.Handler(_routingPort, _dateTimeProvider, _analyzeLogger);
        var command = new AnalyzeNonProdEnvironment.Command(
            TenantId: "tenant-acme-001",
            EnvironmentId: "env-qa-001",
            EnvironmentName: "QA",
            EnvironmentProfile: "qa",
            ServiceFilter: null,
            ObservationWindowDays: 7,
            PreferredProvider: null);

        var result = await handler.Handle(command, CancellationToken.None);

        // Risk must be detected
        result.IsSuccess.Should().BeTrue();
        result.Value!.OverallRiskLevel.Should().Be("HIGH");
        result.Value.Findings.Should().HaveCount(2);
        result.Value.Findings[0].Severity.Should().Be("HIGH");
        result.Value.Findings[0].Category.Should().Be("contract-drift");
        result.Value.Findings[1].Severity.Should().Be("MEDIUM");
        result.Value.Recommendation.Should().Contain("Block");
        // Context must be preserved in response
        result.Value.EnvironmentId.Should().Be("env-qa-001");
        result.Value.TenantId.Should().Be("tenant-acme-001");
    }

    // ─── SCENARIO: UAT vs PROD COMPARISON ────────────────────────────────────

    [Fact]
    public async Task Scenario_UATvsProdComparison_ShouldDetectRegressionAndBlockPromotion()
    {
        // SCENARIO: UAT has topology changes compared to production.
        // The AI compares UAT (subject) vs PROD (reference) and detects divergences.
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(
                "DIVERGENCE: HIGH | topology | catalog-service is missing in UAT but present in PROD\n" +
                "DIVERGENCE: HIGH | contracts | notifications-service has schema mismatch (UAT v3 vs PROD v2)\n" +
                "DIVERGENCE: LOW | telemetry | P99 latency slightly higher in UAT (within acceptable range)\n" +
                "PROMOTION_RECOMMENDATION: BLOCK_PROMOTION\n" +
                "SUMMARY: UAT has critical topology gap and schema mismatch. Promotion must be blocked.");

        var handler = new CompareEnvironments.Handler(_routingPort, _dateTimeProvider, _compareLogger);
        var command = new CompareEnvironments.Command(
            TenantId: "tenant-acme-001",
            SubjectEnvironmentId: "env-uat-001",
            SubjectEnvironmentName: "UAT",
            SubjectEnvironmentProfile: "uat",
            ReferenceEnvironmentId: "env-prod-001",
            ReferenceEnvironmentName: "Production",
            ReferenceEnvironmentProfile: "production",
            ServiceFilter: null,
            ComparisonDimensions: null,
            PreferredProvider: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PromotionRecommendation.Should().Be("BLOCK_PROMOTION");
        result.Value.Divergences.Should().HaveCount(3);
        result.Value.Divergences.Where(d => d.Severity == "HIGH").Should().HaveCount(2);
        result.Value.Divergences.Should().Contain(d => d.Dimension == "topology");
        result.Value.Divergences.Should().Contain(d => d.Dimension == "contracts");
        result.Value.Summary.Should().ContainEquivalentOf("block");
    }

    // ─── SCENARIO: STAGING → PROD PROMOTION READINESS ────────────────────────

    [Fact]
    public async Task Scenario_StagingPromotionReadiness_ShouldBlockWhenContractBreaking()
    {
        // SCENARIO: payment-service v2.1.0 is in STAGING and being assessed for promotion to PROD.
        // The AI finds a blocking contract issue and recommends blocking.
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(
                "READINESS_SCORE: 42\n" +
                "READINESS_LEVEL: NOT_READY\n" +
                "BLOCKER: contract | Response schema for POST /payments changed incompatibly — 3 consumers affected\n" +
                "BLOCKER: tests | Integration test suite has 8 failures in checkout flow\n" +
                "WARNING: performance | P95 latency increased 23% vs previous version\n" +
                "SHOULD_BLOCK: YES\n" +
                "SUMMARY: Critical blockers prevent safe promotion. Resolve contract and test failures first.");

        var handler = new AssessPromotionReadiness.Handler(_routingPort, _dateTimeProvider, _readinessLogger);
        var command = new AssessPromotionReadiness.Command(
            TenantId: "tenant-acme-001",
            SourceEnvironmentId: "env-staging-001",
            SourceEnvironmentName: "Staging",
            SourceIsProductionLike: false,
            TargetEnvironmentId: "env-prod-001",
            TargetEnvironmentName: "Production",
            TargetIsProductionLike: true,
            ServiceName: "payment-service",
            Version: "2.1.0",
            ReleaseId: "rel-789",
            ObservationWindowDays: 7,
            PreferredProvider: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReadinessScore.Should().Be(42);
        result.Value.ReadinessLevel.Should().Be("NOT_READY");
        result.Value.ShouldBlock.Should().BeTrue();
        result.Value.Blockers.Should().HaveCount(2);
        result.Value.Warnings.Should().HaveCount(1);
        result.Value.Blockers[0].Category.Should().Be("contract");
        result.Value.Blockers[1].Category.Should().Be("tests");
        result.Value.Warnings[0].Category.Should().Be("performance");
    }

    [Fact]
    public async Task Scenario_StagingPromotionReadiness_ShouldApproveSafeService()
    {
        // SCENARIO: api-gateway v1.5.0 is in STAGING and ready for promotion.
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(
                "READINESS_SCORE: 94\n" +
                "READINESS_LEVEL: READY\n" +
                "SHOULD_BLOCK: NO\n" +
                "SUMMARY: All checks pass. Service is ready for production promotion.");

        var handler = new AssessPromotionReadiness.Handler(_routingPort, _dateTimeProvider, _readinessLogger);
        var command = new AssessPromotionReadiness.Command(
            "tenant-acme-001", "env-staging-001", "Staging", false, "env-prod-001", "Production", true,
            "api-gateway", "1.5.0", null, 7, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReadinessLevel.Should().Be("READY");
        result.Value.ShouldBlock.Should().BeFalse();
        result.Value.ReadinessScore.Should().BeGreaterThan(80);
        result.Value.Blockers.Should().BeEmpty();
    }

    // ─── SCENARIO: HML ENVIRONMENT ANALYSIS ──────────────────────────────────

    [Fact]
    public async Task Scenario_HMLAnalysis_MediumRisk_ShouldRequireReview()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(
                "FINDING: MEDIUM | telemetry | checkout-service error rate elevated (8% vs 2% baseline)\n" +
                "FINDING: LOW | topology | recommendation-service has 1 degraded instance\n" +
                "OVERALL_RISK: MEDIUM\n" +
                "RECOMMENDATION: Review error rate spike in checkout-service before proceeding. Monitor for 48h.");

        var handler = new AnalyzeNonProdEnvironment.Handler(_routingPort, _dateTimeProvider, _analyzeLogger);
        var command = new AnalyzeNonProdEnvironment.Command(
            "tenant-acme-001", "env-hml-001", "HML", "hml", null, 3, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OverallRiskLevel.Should().Be("MEDIUM");
        result.Value.Findings.Should().HaveCount(2);
        result.Value.EnvironmentProfile.Should().Be("hml");
        result.Value.ObservationWindowDays.Should().Be(3);
    }

    // ─── SCENARIO: DEV ENVIRONMENT — LOW RISK ────────────────────────────────

    [Fact]
    public async Task Scenario_DevEnvironmentAnalysis_LowRisk_ShouldBeIdentifiedAsNonCritical()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _routingPort.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(
                "OVERALL_RISK: LOW\n" +
                "RECOMMENDATION: No critical signals. Environment is in expected development state.");

        var handler = new AnalyzeNonProdEnvironment.Handler(_routingPort, _dateTimeProvider, _analyzeLogger);
        var command = new AnalyzeNonProdEnvironment.Command(
            "tenant-acme-001", "env-dev-001", "Development", "development", null, 1, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OverallRiskLevel.Should().Be("LOW");
        result.Value.Findings.Should().BeEmpty();
        result.Value.EnvironmentProfile.Should().Be("development");
        result.Value.IsFallback.Should().BeFalse();
    }

    // ─── SCENARIO: PROMOTION READINESS WITH RELEASE ID ───────────────────────

    [Fact]
    public async Task Scenario_PromotionReadinessWithReleaseId_ShouldCarryReleaseContextThrough()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        string? capturedGrounding = null;
        _routingPort.RouteQueryAsync(
            Arg.Do<string>(g => capturedGrounding = g),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("READINESS_SCORE: 80\nREADINESS_LEVEL: NEEDS_REVIEW\nSHOULD_BLOCK: NO\nSUMMARY: Minor concerns only.");

        var handler = new AssessPromotionReadiness.Handler(_routingPort, _dateTimeProvider, _readinessLogger);
        var command = new AssessPromotionReadiness.Command(
            "tenant-acme-001", "env-qa-001", "QA", false, "env-prod-001", "Production", true,
            "inventory-service", "4.0.0", "rel-release-final-001", 14, null);

        var result = await handler.Handle(command, CancellationToken.None);

        // Release ID must be propagated to grounding (for traceability)
        capturedGrounding.Should().Contain("rel-release-final-001");
        result.IsSuccess.Should().BeTrue();
        result.Value!.ReleaseId.Should().Be("rel-release-final-001");
    }

    // ─── SCENARIO: MULTI-DIMENSIONAL ENVIRONMENT COMPARISON ──────────────────

    [Fact]
    public async Task Scenario_CompareWithSpecificDimensions_ShouldFocusAnalysis()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        string? capturedGrounding = null;
        _routingPort.RouteQueryAsync(
            Arg.Do<string>(g => capturedGrounding = g),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("PROMOTION_RECOMMENDATION: REVIEW_REQUIRED\nSUMMARY: Some contract drift found.");

        var handler = new CompareEnvironments.Handler(_routingPort, _dateTimeProvider, _compareLogger);
        var command = new CompareEnvironments.Command(
            "tenant-acme-001",
            "env-qa-001", "QA", "qa",
            "env-prod-001", "Production", "production",
            null,
            new List<string> { "contracts", "telemetry" }.AsReadOnly(),
            null);

        await handler.Handle(command, CancellationToken.None);

        capturedGrounding.Should().Contain("contracts");
        capturedGrounding.Should().Contain("telemetry");
    }
}
