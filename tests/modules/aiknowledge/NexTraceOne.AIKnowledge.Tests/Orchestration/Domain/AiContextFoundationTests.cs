using System.Linq;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Context;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Orchestration.Domain;

/// <summary>
/// Testes para a fundação de contexto de IA da Fase 1.
/// Cobre AiExecutionContext, PromotionRiskAnalysisContext, EnvironmentComparisonContext,
/// RiskFinding, RegressionSignal e ReadinessAssessment.
/// </summary>
public sealed class AiContextFoundationTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EnvironmentId StagingEnvId = EnvironmentId.New();
    private static readonly EnvironmentId ProdEnvId = EnvironmentId.New();

    private static AiUserContext MakeUser() =>
        new("user-1", "John Dev", "Engineer", ["developer"]);

    private static AiExecutionContext MakeExecutionContext(bool isProductionLike = false) =>
        AiExecutionContext.Create(
            TenantId,
            isProductionLike ? ProdEnvId : StagingEnvId,
            isProductionLike ? EnvironmentProfile.Production : EnvironmentProfile.Staging,
            isProductionLike,
            MakeUser(),
            "change-governance",
            AiDataScope.FullAnalysisScopes);

    // ─────────────────────────────────────────────────────────────
    // AiExecutionContext
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void AiExecutionContext_Create_Should_SetDefaultScopesAndTimeWindow_WhenNotProvided()
    {
        var ctx = AiExecutionContext.Create(
            TenantId, StagingEnvId, EnvironmentProfile.Staging, false, MakeUser(), "incidents");

        ctx.AllowedDataScopes.Should().BeEquivalentTo(AiDataScope.DefaultScopes);
        ctx.TimeWindow.Should().NotBeNull();
        ctx.ReleaseContext.Should().BeNull();
    }

    [Fact]
    public void AiExecutionContext_CanUseCrossEnvironmentComparison_Should_BeFalse_ForProduction()
    {
        var ctx = MakeExecutionContext(isProductionLike: true);

        ctx.CanUseCrossEnvironmentComparison().Should().BeFalse();
    }

    [Fact]
    public void AiExecutionContext_CanUseCrossEnvironmentComparison_Should_BeTrue_ForNonProduction_WithScope()
    {
        var ctx = MakeExecutionContext(isProductionLike: false);

        ctx.CanUseCrossEnvironmentComparison().Should().BeTrue();
    }

    [Fact]
    public void AiExecutionContext_CanAnalyzePromotionReadiness_Should_BeTrue_WhenScopeAllows()
    {
        var ctx = MakeExecutionContext(isProductionLike: false);

        ctx.CanAnalyzePromotionReadiness().Should().BeTrue();
    }

    [Fact]
    public void AiExecutionContext_Equality_Should_BeBasedOnTenantEnvironmentAndUser()
    {
        var ctx1 = AiExecutionContext.Create(TenantId, StagingEnvId, EnvironmentProfile.Staging, false, MakeUser(), "module-a");
        var ctx2 = AiExecutionContext.Create(TenantId, StagingEnvId, EnvironmentProfile.Staging, false, MakeUser(), "module-b");

        ctx1.Should().Be(ctx2);
    }

    // ─────────────────────────────────────────────────────────────
    // PromotionRiskAnalysisContext
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void PromotionRiskAnalysisContext_Create_Should_SetAllFields()
    {
        var execCtx = MakeExecutionContext();
        var ctx = PromotionRiskAnalysisContext.Create(
            execCtx,
            StagingEnvId, EnvironmentProfile.Staging,
            ProdEnvId, EnvironmentProfile.Production,
            "payment-service", "2.5.0");

        ctx.ServiceName.Should().Be("payment-service");
        ctx.Version.Should().Be("2.5.0");
        ctx.SourceEnvironmentId.Should().Be(StagingEnvId);
        ctx.TargetEnvironmentId.Should().Be(ProdEnvId);
        ctx.IsPromotionToProduction().Should().BeTrue();
    }

    [Fact]
    public void PromotionRiskAnalysisContext_Create_Should_Fail_WhenSourceEqualsTarget()
    {
        var execCtx = MakeExecutionContext();
        var act = () => PromotionRiskAnalysisContext.Create(
            execCtx,
            StagingEnvId, EnvironmentProfile.Staging,
            StagingEnvId, EnvironmentProfile.Staging,
            "service", "1.0.0");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PromotionRiskAnalysisContext_IsPromotionToProduction_Should_BeFalse_ForNonProduction()
    {
        var execCtx = MakeExecutionContext();
        var qaEnvId = EnvironmentId.New();
        var ctx = PromotionRiskAnalysisContext.Create(
            execCtx,
            StagingEnvId, EnvironmentProfile.Staging,
            qaEnvId, EnvironmentProfile.Validation,
            "service", "1.0.0");

        ctx.IsPromotionToProduction().Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────
    // EnvironmentComparisonContext
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void EnvironmentComparisonContext_Create_Should_SetDefaultDimensions_WhenNoneProvided()
    {
        var execCtx = MakeExecutionContext();
        var ctx = EnvironmentComparisonContext.Create(
            execCtx, StagingEnvId, EnvironmentProfile.Staging, ProdEnvId, EnvironmentProfile.Production);

        ctx.Dimensions.Should().BeEquivalentTo(ComparisonDimensionExtensions.AllDimensions);
        ctx.ServiceFilter.Should().BeEmpty();
    }

    [Fact]
    public void EnvironmentComparisonContext_Create_Should_Fail_WhenSubjectEqualsReference()
    {
        var execCtx = MakeExecutionContext();
        var act = () => EnvironmentComparisonContext.Create(
            execCtx, StagingEnvId, EnvironmentProfile.Staging, StagingEnvId, EnvironmentProfile.Staging);

        act.Should().Throw<InvalidOperationException>();
    }

    // ─────────────────────────────────────────────────────────────
    // RiskFinding
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void RiskFinding_Create_Should_SetAllFields()
    {
        var finding = RiskFinding.Create(
            RiskCategory.PerformanceRegression,
            RiskSeverity.High,
            "Latência acima do esperado",
            "P95 latência 420ms vs baseline 180ms.",
            Now,
            affectedService: "payment-service",
            evidenceReferences: ["trace-abc123"],
            suggestedAction: "Investigar novo índice de banco.");

        finding.Category.Should().Be(RiskCategory.PerformanceRegression);
        finding.Severity.Should().Be(RiskSeverity.High);
        finding.AffectedService.Should().Be("payment-service");
        finding.EvidenceReferences.Should().HaveCount(1);
        finding.FindingId.Should().NotBeEmpty();
    }

    [Fact]
    public void RiskFinding_Create_Should_Fail_WhenTitleIsEmpty()
    {
        var act = () => RiskFinding.Create(RiskCategory.DataAnomaly, RiskSeverity.Warning, "", "desc", Now);

        act.Should().Throw<ArgumentException>();
    }

    // ─────────────────────────────────────────────────────────────
    // RegressionSignal
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void RegressionSignal_Create_Should_CalculateDeltaAndIntensity_Correctly()
    {
        var signal = RegressionSignal.Create("svc", "latency_p95_ms", 420, 180, "ms", Now);

        signal.DeltaPercent.Should().BeApproximately(133.33, 0.1);
        signal.IsDegradation.Should().BeTrue();
        signal.Intensity.Should().Be(RegressionIntensity.Severe);
    }

    [Fact]
    public void RegressionSignal_Create_Should_DetectNoDegradation_WhenMetricImproves()
    {
        var signal = RegressionSignal.Create("svc", "latency_p95_ms", 100, 200, "ms", Now);

        signal.IsDegradation.Should().BeFalse();
    }

    [Fact]
    public void RegressionSignal_Create_Should_DetectDegradation_ForThroughputDecrease_WhenHigherIsBetter()
    {
        var signal = RegressionSignal.Create("svc", "throughput_rps", 50, 100, "req/s", Now, higherIsBetter: true);

        signal.IsDegradation.Should().BeTrue();
        signal.Intensity.Should().Be(RegressionIntensity.Severe);
    }

    [Theory]
    [InlineData(100, 100, RegressionIntensity.Negligible)]
    [InlineData(108, 100, RegressionIntensity.Minor)]
    [InlineData(120, 100, RegressionIntensity.Moderate)]
    [InlineData(140, 100, RegressionIntensity.Significant)]
    [InlineData(200, 100, RegressionIntensity.Severe)]
    public void RegressionSignal_Create_Should_MapIntensity_FromDelta(
        double current, double baseline, RegressionIntensity expectedIntensity)
    {
        var signal = RegressionSignal.Create("svc", "latency", current, baseline, "ms", Now);

        signal.Intensity.Should().Be(expectedIntensity);
    }

    // ─────────────────────────────────────────────────────────────
    // ReadinessAssessment
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void ReadinessAssessment_Create_Should_RecommendPromote_WhenNoFindings()
    {
        var assessment = ReadinessAssessment.Create(
            TenantId, StagingEnvId, ProdEnvId,
            "payment-service", "2.5.0",
            riskFindings: [],
            regressionSignals: [],
            executiveSummary: "Sem achados de risco.",
            Now);

        assessment.ReadinessScore.Should().Be(100);
        assessment.Recommendation.Should().Be(PromotionRecommendation.Promote);
        assessment.AssessmentId.Should().NotBeEmpty();
    }

    [Fact]
    public void ReadinessAssessment_Create_Should_Block_WhenCriticalFindingExists()
    {
        var criticalFinding = RiskFinding.Create(
            RiskCategory.ContractBreakingChange,
            RiskSeverity.Critical,
            "Breaking change detectada",
            "API removeu campo obrigatório.",
            Now);

        var assessment = ReadinessAssessment.Create(
            TenantId, StagingEnvId, ProdEnvId,
            "payment-service", "2.5.0",
            riskFindings: [criticalFinding],
            regressionSignals: [],
            executiveSummary: "Breaking change bloqueia promoção.",
            Now);

        assessment.Recommendation.Should().Be(PromotionRecommendation.Block);
        assessment.ReadinessScore.Should().Be(60);
    }

    [Fact]
    public void ReadinessAssessment_Create_Should_PromoteWithCaution_ForModerateRisk()
    {
        var warnings = System.Linq.Enumerable.Range(0, 2).Select(_ =>
            RiskFinding.Create(RiskCategory.DependencyRisk, RiskSeverity.Warning, "Aviso", "Desc", Now)).ToList();

        var assessment = ReadinessAssessment.Create(
            TenantId, StagingEnvId, ProdEnvId,
            "service", "1.0.0",
            riskFindings: warnings,
            regressionSignals: [],
            executiveSummary: "Dois avisos menores.",
            Now);

        assessment.ReadinessScore.Should().Be(90);
        assessment.Recommendation.Should().Be(PromotionRecommendation.Promote);
    }

    [Fact]
    public void ReadinessAssessment_Create_Should_DeductPointsForSevereRegressions()
    {
        var severeSignal = RegressionSignal.Create("svc", "latency", 600, 100, "ms", Now);

        var assessment = ReadinessAssessment.Create(
            TenantId, StagingEnvId, ProdEnvId,
            "service", "1.0.0",
            riskFindings: [],
            regressionSignals: [severeSignal],
            executiveSummary: "Regressão severa de latência.",
            Now);

        assessment.ReadinessScore.Should().Be(90);
        assessment.Recommendation.Should().Be(PromotionRecommendation.Promote);
        assessment.RegressionSignals.Should().HaveCount(1);
    }
}
