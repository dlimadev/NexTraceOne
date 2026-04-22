using System.Linq;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetCrossTenantMaturityReport;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetTenantHealthScoreReport;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetPlatformPolicyComplianceReport;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.ChangeGovernance.Tests.Compliance;

/// <summary>
/// Testes unitários para Wave AJ — Multi-Tenant Governance Intelligence.
/// AJ.1: GetCrossTenantMaturityReport (14 testes)
/// AJ.2: GetTenantHealthScoreReport   (16 testes)
/// AJ.3: GetPlatformPolicyComplianceReport (15 testes)
/// </summary>
public sealed class WaveAjMultiTenantGovernanceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 22, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-aj-test";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AJ.1 — GetCrossTenantMaturityReport
    // ═══════════════════════════════════════════════════════════════════════

    private static ICrossTenantMaturityReader.TenantMaturityDimensions MakeDims(
        string tenantId = TenantId,
        decimal contractGoverned = 80m,
        decimal changeConfidence = 70m,
        decimal sloTracked = 60m,
        decimal runbookCovered = 50m,
        decimal profilingActive = 40m,
        decimal complianceEvaluated = 30m,
        decimal aiAssistantUsed = 20m)
        => new(tenantId, contractGoverned, changeConfidence, sloTracked,
            runbookCovered, profilingActive, complianceEvaluated, aiAssistantUsed);

    private static GetCrossTenantMaturityReport.Handler CreateMaturityHandler(
        ICrossTenantMaturityReader.TenantMaturityDimensions dims,
        IReadOnlyList<ICrossTenantMaturityReader.TenantMaturityDimensions> ecosystemDims)
    {
        var reader = Substitute.For<ICrossTenantMaturityReader>();
        reader.GetDimensionsAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(dims);
        reader.ListConsentedTenantDimensionsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ecosystemDims);
        return new GetCrossTenantMaturityReport.Handler(reader, CreateClock());
    }

    [Fact]
    public async Task AJ1_Handle_AllDims50_Returns_DevelopingTier()
    {
        var dims = MakeDims(contractGoverned: 50m, changeConfidence: 50m, sloTracked: 50m,
            runbookCovered: 50m, profilingActive: 50m, complianceEvaluated: 50m, aiAssistantUsed: 50m);
        var handler = CreateMaturityHandler(dims, []);
        var result = await handler.Handle(new GetCrossTenantMaturityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantMaturityScore.Should().Be(50m);
        result.Value.Tier.Should().Be(GetCrossTenantMaturityReport.MaturityTier.Developing);
    }

    [Fact]
    public async Task AJ1_Handle_AllDims90_Returns_PioneerTier()
    {
        var dims = MakeDims(contractGoverned: 90m, changeConfidence: 90m, sloTracked: 90m,
            runbookCovered: 90m, profilingActive: 90m, complianceEvaluated: 90m, aiAssistantUsed: 90m);
        var handler = CreateMaturityHandler(dims, []);
        var result = await handler.Handle(new GetCrossTenantMaturityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetCrossTenantMaturityReport.MaturityTier.Pioneer);
    }

    [Fact]
    public async Task AJ1_Handle_AllDims20_Returns_EmergingTier()
    {
        var dims = MakeDims(contractGoverned: 20m, changeConfidence: 20m, sloTracked: 20m,
            runbookCovered: 20m, profilingActive: 20m, complianceEvaluated: 20m, aiAssistantUsed: 20m);
        var handler = CreateMaturityHandler(dims, []);
        var result = await handler.Handle(new GetCrossTenantMaturityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetCrossTenantMaturityReport.MaturityTier.Emerging);
    }

    [Fact]
    public async Task AJ1_Handle_AllDims70_Returns_AdvancedTier()
    {
        var dims = MakeDims(contractGoverned: 70m, changeConfidence: 70m, sloTracked: 70m,
            runbookCovered: 70m, profilingActive: 70m, complianceEvaluated: 70m, aiAssistantUsed: 70m);
        var handler = CreateMaturityHandler(dims, []);
        var result = await handler.Handle(new GetCrossTenantMaturityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetCrossTenantMaturityReport.MaturityTier.Advanced);
    }

    [Fact]
    public async Task AJ1_Handle_InsufficientPeers_ReturnsInsufficientFlag()
    {
        var dims = MakeDims();
        var handler = CreateMaturityHandler(dims, []); // 0 peers < minTenantsForBenchmark=5
        var result = await handler.Handle(
            new GetCrossTenantMaturityReport.Query(TenantId, MinTenantsForBenchmark: 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.InsufficientBenchmarkPeers.Should().BeTrue();
        result.Value.BenchmarkPercentile.Should().BeNull();
        result.Value.Benchmark.Should().BeNull();
    }

    [Fact]
    public async Task AJ1_Handle_SufficientPeers_Returns_BenchmarkPercentile()
    {
        var dims = MakeDims(contractGoverned: 80m, changeConfidence: 80m, sloTracked: 80m,
            runbookCovered: 80m, profilingActive: 80m, complianceEvaluated: 80m, aiAssistantUsed: 80m);

        // 5 peers with varying scores: 20, 40, 60, 70, 90 → avg 56
        var peers = new[]
        {
            MakeDims("t1", 20m, 20m, 20m, 20m, 20m, 20m, 20m),
            MakeDims("t2", 40m, 40m, 40m, 40m, 40m, 40m, 40m),
            MakeDims("t3", 60m, 60m, 60m, 60m, 60m, 60m, 60m),
            MakeDims("t4", 70m, 70m, 70m, 70m, 70m, 70m, 70m),
            MakeDims("t5", 90m, 90m, 90m, 90m, 90m, 90m, 90m)
        };

        var handler = CreateMaturityHandler(dims, peers);
        var result = await handler.Handle(
            new GetCrossTenantMaturityReport.Query(TenantId, MinTenantsForBenchmark: 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.InsufficientBenchmarkPeers.Should().BeFalse();
        result.Value.BenchmarkPercentile.Should().NotBeNull();
        result.Value.Benchmark.Should().NotBeNull();
        result.Value.Benchmark!.ParticipatingTenants.Should().Be(5);
    }

    [Fact]
    public async Task AJ1_Handle_SelfExcludedFromPeerSet()
    {
        var dims = MakeDims(TenantId); // Self in ecosystem list should be excluded
        var ecosystemDims = new[]
        {
            MakeDims(TenantId),       // should be excluded
            MakeDims("peer1", 50m, 50m, 50m, 50m, 50m, 50m, 50m),
            MakeDims("peer2", 60m, 60m, 60m, 60m, 60m, 60m, 60m),
            MakeDims("peer3", 70m, 70m, 70m, 70m, 70m, 70m, 70m),
            MakeDims("peer4", 80m, 80m, 80m, 80m, 80m, 80m, 80m),
            MakeDims("peer5", 90m, 90m, 90m, 90m, 90m, 90m, 90m)
        };

        var handler = CreateMaturityHandler(dims, ecosystemDims);
        var result = await handler.Handle(
            new GetCrossTenantMaturityReport.Query(TenantId, MinTenantsForBenchmark: 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Benchmark!.ParticipatingTenants.Should().Be(5); // self excluded
    }

    [Fact]
    public async Task AJ1_Handle_WeakestDimensions_IdentifiesTopGaps()
    {
        var dims = MakeDims(contractGoverned: 20m, changeConfidence: 90m, sloTracked: 90m,
            runbookCovered: 90m, profilingActive: 90m, complianceEvaluated: 90m, aiAssistantUsed: 20m);

        var peers = Enumerable.Range(1, 5)
            .Select(i => MakeDims($"p{i}", 80m, 80m, 80m, 80m, 80m, 80m, 80m))
            .ToArray();

        var handler = CreateMaturityHandler(dims, peers);
        var result = await handler.Handle(
            new GetCrossTenantMaturityReport.Query(TenantId, MinTenantsForBenchmark: 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WeakestDimensions.Should().Contain("ContractGoverned");
        result.Value.WeakestDimensions.Should().Contain("AiAssistantUsed");
    }

    [Fact]
    public async Task AJ1_Handle_ImprovementPotential_IsPositiveWhenWeakDims()
    {
        var dims = MakeDims(contractGoverned: 10m, changeConfidence: 10m, sloTracked: 80m,
            runbookCovered: 80m, profilingActive: 80m, complianceEvaluated: 80m, aiAssistantUsed: 80m);
        var peers = Enumerable.Range(1, 5)
            .Select(i => MakeDims($"p{i}", 70m, 70m, 70m, 70m, 70m, 70m, 70m))
            .ToArray();

        var handler = CreateMaturityHandler(dims, peers);
        var result = await handler.Handle(
            new GetCrossTenantMaturityReport.Query(TenantId, MinTenantsForBenchmark: 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ImprovementPotential.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task AJ1_Handle_DimensionsCount_Is7()
    {
        var dims = MakeDims();
        var handler = CreateMaturityHandler(dims, []);
        var result = await handler.Handle(new GetCrossTenantMaturityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Dimensions.Should().HaveCount(7);
    }

    [Fact]
    public void AJ1_ClassifyTier_Score85_ReturnsPioneer()
        => GetCrossTenantMaturityReport.Handler.ClassifyTier(85m).Should()
            .Be(GetCrossTenantMaturityReport.MaturityTier.Pioneer);

    [Fact]
    public void AJ1_ClassifyTier_Score849_ReturnsAdvanced()
        => GetCrossTenantMaturityReport.Handler.ClassifyTier(84.9m).Should()
            .Be(GetCrossTenantMaturityReport.MaturityTier.Advanced);

    [Fact]
    public void AJ1_ClassifyTier_Score65_ReturnsAdvanced()
        => GetCrossTenantMaturityReport.Handler.ClassifyTier(65m).Should()
            .Be(GetCrossTenantMaturityReport.MaturityTier.Advanced);

    [Fact]
    public void AJ1_ClassifyTier_Score649_ReturnsDeveloping()
        => GetCrossTenantMaturityReport.Handler.ClassifyTier(64.9m).Should()
            .Be(GetCrossTenantMaturityReport.MaturityTier.Developing);

    [Fact]
    public void AJ1_ClassifyTier_Score40_ReturnsDeveloping()
        => GetCrossTenantMaturityReport.Handler.ClassifyTier(40m).Should()
            .Be(GetCrossTenantMaturityReport.MaturityTier.Developing);

    [Fact]
    public void AJ1_ClassifyTier_Score399_ReturnsEmerging()
        => GetCrossTenantMaturityReport.Handler.ClassifyTier(39.9m).Should()
            .Be(GetCrossTenantMaturityReport.MaturityTier.Emerging);

    [Fact]
    public async Task AJ1_Validator_EmptyTenantId_IsInvalid()
    {
        var validator = new GetCrossTenantMaturityReport.Validator();
        var result = await validator.ValidateAsync(new GetCrossTenantMaturityReport.Query(string.Empty));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AJ1_Validator_LookbackMonthsOutOfRange_IsInvalid()
    {
        var validator = new GetCrossTenantMaturityReport.Validator();
        var result = await validator.ValidateAsync(new GetCrossTenantMaturityReport.Query(TenantId, LookbackMonths: 13));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AJ.2 — GetTenantHealthScoreReport
    // ═══════════════════════════════════════════════════════════════════════

    private static ITenantHealthDataReader.TenantHealthPillarData MakePillarData(
        string tenantId = TenantId,
        decimal sg = 80m, decimal cc = 80m, decimal or = 80m,
        decimal ch = 80m, decimal cov = 80m, decimal fo = 80m)
        => new(tenantId, sg, cc, or, ch, cov, fo);

    private static GetTenantHealthScoreReport.Handler CreateHealthHandler(
        ITenantHealthDataReader.TenantHealthPillarData current,
        ITenantHealthDataReader.TenantHealthPillarData? previous = null)
    {
        var reader = Substitute.For<ITenantHealthDataReader>();
        var callCount = 0;
        reader.GetPillarDataAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return callCount == 1 ? current : (previous ?? MakePillarData());
            });
        return new GetTenantHealthScoreReport.Handler(reader, CreateClock());
    }

    [Fact]
    public async Task AJ2_Handle_AllPillars80_Returns_GoodTier()
    {
        var handler = CreateHealthHandler(MakePillarData());
        var result = await handler.Handle(new GetTenantHealthScoreReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetTenantHealthScoreReport.HealthTier.Good);
        result.Value.TenantHealthScore.Should().Be(80m);
    }

    [Fact]
    public async Task AJ2_Handle_AllPillars100_Returns_ExcellentTier()
    {
        var handler = CreateHealthHandler(MakePillarData(sg: 100m, cc: 100m, or: 100m, ch: 100m, cov: 100m, fo: 100m));
        var result = await handler.Handle(new GetTenantHealthScoreReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetTenantHealthScoreReport.HealthTier.Excellent);
        result.Value.TenantHealthScore.Should().Be(100m);
    }

    [Fact]
    public async Task AJ2_Handle_AllPillars30_Returns_AtRiskTier()
    {
        var handler = CreateHealthHandler(MakePillarData(sg: 30m, cc: 30m, or: 30m, ch: 30m, cov: 30m, fo: 30m));
        var result = await handler.Handle(new GetTenantHealthScoreReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetTenantHealthScoreReport.HealthTier.AtRisk);
    }

    [Fact]
    public async Task AJ2_Handle_AllPillars50_Returns_FairTier()
    {
        var handler = CreateHealthHandler(MakePillarData(sg: 50m, cc: 50m, or: 50m, ch: 50m, cov: 50m, fo: 50m));
        var result = await handler.Handle(new GetTenantHealthScoreReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetTenantHealthScoreReport.HealthTier.Fair);
    }

    [Fact]
    public async Task AJ2_Handle_PillarBreakdown_Has6Pillars()
    {
        var handler = CreateHealthHandler(MakePillarData());
        var result = await handler.Handle(new GetTenantHealthScoreReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PillarBreakdown.Should().HaveCount(6);
    }

    [Fact]
    public async Task AJ2_Handle_WeightedScore_IsCorrect()
    {
        // sg=100 (20%), cc=0 (20%), or=100 (20%), ch=0 (15%), cov=100 (15%), fo=0 (10%)
        // = 100*0.20 + 0 + 100*0.20 + 0 + 100*0.15 + 0 = 20 + 20 + 15 = 55
        var data = MakePillarData(sg: 100m, cc: 0m, or: 100m, ch: 0m, cov: 100m, fo: 0m);
        var handler = CreateHealthHandler(data);
        var result = await handler.Handle(new GetTenantHealthScoreReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantHealthScore.Should().Be(55m);
    }

    [Fact]
    public async Task AJ2_Handle_TrendImproving_WhenCurrentHigherThanPrevious()
    {
        var current = MakePillarData(sg: 80m, cc: 80m, or: 80m, ch: 80m, cov: 80m, fo: 80m);
        var previous = MakePillarData(sg: 60m, cc: 60m, or: 60m, ch: 60m, cov: 60m, fo: 60m);
        var handler = CreateHealthHandler(current, previous);
        var result = await handler.Handle(new GetTenantHealthScoreReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Trend.Trend.Should().Be("Improving");
        result.Value.Trend.Delta.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task AJ2_Handle_TrendDeclining_WhenCurrentLowerThanPrevious()
    {
        var current = MakePillarData(sg: 50m, cc: 50m, or: 50m, ch: 50m, cov: 50m, fo: 50m);
        var previous = MakePillarData(sg: 80m, cc: 80m, or: 80m, ch: 80m, cov: 80m, fo: 80m);
        var handler = CreateHealthHandler(current, previous);
        var result = await handler.Handle(new GetTenantHealthScoreReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Trend.Trend.Should().Be("Declining");
        result.Value.Trend.Delta.Should().BeLessThan(0m);
    }

    [Fact]
    public async Task AJ2_Handle_TopIssues_OrderedByImpact()
    {
        var data = MakePillarData(sg: 10m, cc: 90m, or: 90m, ch: 90m, cov: 90m, fo: 90m);
        var handler = CreateHealthHandler(data);
        var result = await handler.Handle(new GetTenantHealthScoreReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TopIssues.Should().NotBeEmpty();
        result.Value.TopIssues.First().PillarName.Should().Be("ServiceGovernance");
    }

    [Fact]
    public async Task AJ2_Handle_ActionableItems_NotEmpty_WhenIssuesExist()
    {
        var data = MakePillarData(sg: 10m, cc: 10m, or: 10m, ch: 10m, cov: 10m, fo: 10m);
        var handler = CreateHealthHandler(data);
        var result = await handler.Handle(new GetTenantHealthScoreReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ActionableItems.Should().NotBeEmpty();
    }

    [Fact]
    public void AJ2_ClassifyTier_Score85_ReturnsExcellent()
        => GetTenantHealthScoreReport.Handler.ClassifyTier(85m).Should()
            .Be(GetTenantHealthScoreReport.HealthTier.Excellent);

    [Fact]
    public void AJ2_ClassifyTier_Score849_ReturnsGood()
        => GetTenantHealthScoreReport.Handler.ClassifyTier(84.9m).Should()
            .Be(GetTenantHealthScoreReport.HealthTier.Good);

    [Fact]
    public void AJ2_ClassifyTier_Score65_ReturnsGood()
        => GetTenantHealthScoreReport.Handler.ClassifyTier(65m).Should()
            .Be(GetTenantHealthScoreReport.HealthTier.Good);

    [Fact]
    public void AJ2_ClassifyTier_Score649_ReturnsFair()
        => GetTenantHealthScoreReport.Handler.ClassifyTier(64.9m).Should()
            .Be(GetTenantHealthScoreReport.HealthTier.Fair);

    [Fact]
    public void AJ2_ClassifyTier_Score40_ReturnsFair()
        => GetTenantHealthScoreReport.Handler.ClassifyTier(40m).Should()
            .Be(GetTenantHealthScoreReport.HealthTier.Fair);

    [Fact]
    public void AJ2_ClassifyTier_Score399_ReturnsAtRisk()
        => GetTenantHealthScoreReport.Handler.ClassifyTier(39.9m).Should()
            .Be(GetTenantHealthScoreReport.HealthTier.AtRisk);

    [Fact]
    public async Task AJ2_Validator_LookbackDaysOutOfRange_IsInvalid()
    {
        var validator = new GetTenantHealthScoreReport.Validator();
        var result = await validator.ValidateAsync(new GetTenantHealthScoreReport.Query(TenantId, LookbackDays: 3));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AJ2_Validator_EmptyTenantId_IsInvalid()
    {
        var validator = new GetTenantHealthScoreReport.Validator();
        var result = await validator.ValidateAsync(new GetTenantHealthScoreReport.Query(string.Empty));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AJ.3 — GetPlatformPolicyComplianceReport
    // ═══════════════════════════════════════════════════════════════════════

    private static PolicyDefinition MakePolicy(string name, PolicyDefinitionType type, bool enabled = true)
    {
        var policy = PolicyDefinition.Create(
            tenantId: TenantId,
            name: name,
            description: null,
            policyType: type,
            rulesJson: "[]",
            actionJson: """{"action":"Block","message":"Policy failed."}""",
            appliesTo: "*",
            environmentFilter: null,
            createdByUserId: "admin",
            now: FixedNow);
        if (!enabled) policy.Disable();
        return policy;
    }

    private static IPolicyEvaluationHistoryReader.PolicyEvaluationRecord MakeEval(
        Guid policyId, string entity = "svc-a", string entityType = "service", bool passed = true)
        => new(policyId, entity, entityType, passed, FixedNow.AddMinutes(-10));

    private static GetPlatformPolicyComplianceReport.Handler CreatePolicyHandler(
        IReadOnlyList<PolicyDefinition> policies,
        IReadOnlyList<IPolicyEvaluationHistoryReader.PolicyEvaluationRecord> evaluations)
    {
        var policyRepo = Substitute.For<IPolicyDefinitionRepository>();
        var evalReader = Substitute.For<IPolicyEvaluationHistoryReader>();

        policyRepo.ListByTenantAsync(Arg.Any<string>(), Arg.Any<PolicyDefinitionType?>(), Arg.Any<CancellationToken>())
            .Returns(policies);
        evalReader.ListEvaluationsAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(evaluations);

        return new GetPlatformPolicyComplianceReport.Handler(policyRepo, evalReader, CreateClock());
    }

    [Fact]
    public async Task AJ3_Handle_NoPolicies_Returns_EmptyReport()
    {
        var handler = CreatePolicyHandler([], []);
        var result = await handler.Handle(
            new GetPlatformPolicyComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalPoliciesAnalyzed.Should().Be(0);
        result.Value.TenantPolicyComplianceScore.Should().Be(100m);
        result.Value.EscalationRequired.Should().BeEmpty();
    }

    [Fact]
    public async Task AJ3_Handle_AllEvaluationsPassed_Returns_EnforcedTier()
    {
        var policy = MakePolicy("p1", PolicyDefinitionType.PromotionGate);
        var evals = Enumerable.Range(0, 10)
            .Select(_ => MakeEval(policy.Id.Value, passed: true))
            .ToList();

        var handler = CreatePolicyHandler([policy], evals);
        var result = await handler.Handle(
            new GetPlatformPolicyComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Policies.Single().Tier.Should()
            .Be(GetPlatformPolicyComplianceReport.PolicyComplianceTier.Enforced);
        result.Value.Policies.Single().PassRatePct.Should().Be(100m);
    }

    [Fact]
    public async Task AJ3_Handle_AllEvaluationsFailed_Returns_FailingTier()
    {
        var policy = MakePolicy("p1", PolicyDefinitionType.PromotionGate);
        var evals = Enumerable.Range(0, 10)
            .Select(_ => MakeEval(policy.Id.Value, passed: false))
            .ToList();

        var handler = CreatePolicyHandler([policy], evals);
        var result = await handler.Handle(
            new GetPlatformPolicyComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Policies.Single().Tier.Should()
            .Be(GetPlatformPolicyComplianceReport.PolicyComplianceTier.Failing);
    }

    [Fact]
    public async Task AJ3_Handle_MandatoryPolicyFailing_Is_InEscalationRequired()
    {
        var policy = MakePolicy("mandatory-gate", PolicyDefinitionType.PromotionGate); // Mandatory
        var evals = Enumerable.Range(0, 10)
            .Select(_ => MakeEval(policy.Id.Value, passed: false))
            .ToList();

        var handler = CreatePolicyHandler([policy], evals);
        var result = await handler.Handle(
            new GetPlatformPolicyComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EscalationRequired.Should().HaveCount(1);
        result.Value.EscalationRequired.Single().PolicyName.Should().Be("mandatory-gate");
    }

    [Fact]
    public async Task AJ3_Handle_AdvisoryPolicyFailing_IsNot_InEscalationRequired()
    {
        var policy = MakePolicy("advisory-access", PolicyDefinitionType.AccessControl); // Advisory
        var evals = Enumerable.Range(0, 10)
            .Select(_ => MakeEval(policy.Id.Value, passed: false))
            .ToList();

        var handler = CreatePolicyHandler([policy], evals);
        var result = await handler.Handle(
            new GetPlatformPolicyComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EscalationRequired.Should().BeEmpty();
    }

    [Fact]
    public async Task AJ3_Handle_TenantScore_Weighted_MandatoryWeight2x()
    {
        // Mandatory (2x): 100% pass
        // Advisory (1x): 0% pass
        // Score = (100*2 + 0*1) / (2+1) ≈ 66.7
        var mandatory = MakePolicy("m1", PolicyDefinitionType.ComplianceCheck);
        var advisory = MakePolicy("a1", PolicyDefinitionType.FreezeWindow);
        var evals = new List<IPolicyEvaluationHistoryReader.PolicyEvaluationRecord>
        {
            MakeEval(mandatory.Id.Value, passed: true),
            MakeEval(advisory.Id.Value, passed: false)
        };

        var handler = CreatePolicyHandler([mandatory, advisory], evals);
        var result = await handler.Handle(
            new GetPlatformPolicyComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantPolicyComplianceScore.Should().BeApproximately(66.7m, 0.5m);
    }

    [Fact]
    public async Task AJ3_Handle_NoEvaluations_PolicyHas100PercentPassRate()
    {
        var policy = MakePolicy("p-noevals", PolicyDefinitionType.AccessControl);
        var handler = CreatePolicyHandler([policy], []);
        var result = await handler.Handle(
            new GetPlatformPolicyComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Policies.Single().PassRatePct.Should().Be(100m);
        result.Value.Policies.Single().EvaluationCount.Should().Be(0);
    }

    [Fact]
    public async Task AJ3_Handle_ViolatingEntities_AreBuiltCorrectly()
    {
        var policy = MakePolicy("p1", PolicyDefinitionType.PromotionGate);
        var evals = new List<IPolicyEvaluationHistoryReader.PolicyEvaluationRecord>
        {
            MakeEval(policy.Id.Value, "svc-a", "service", passed: true),
            MakeEval(policy.Id.Value, "svc-a", "service", passed: false),
            MakeEval(policy.Id.Value, "svc-b", "service", passed: false),
            MakeEval(policy.Id.Value, "svc-b", "service", passed: false)
        };

        var handler = CreatePolicyHandler([policy], evals);
        var result = await handler.Handle(
            new GetPlatformPolicyComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var entry = result.Value!.Policies.Single();
        entry.ViolatingEntities.Should().HaveCount(2);
        entry.ViolatingEntities.Should().Contain(v => v.EntityName == "svc-a");
        entry.ViolatingEntities.Should().Contain(v => v.EntityName == "svc-b");
    }

    [Fact]
    public async Task AJ3_Handle_Distribution_CountsAllTiers()
    {
        var p1 = MakePolicy("enforced", PolicyDefinitionType.PromotionGate); // 100% pass
        var p2 = MakePolicy("failing", PolicyDefinitionType.ComplianceCheck); // 0% pass

        var evals = new List<IPolicyEvaluationHistoryReader.PolicyEvaluationRecord>
        {
            MakeEval(p1.Id.Value, passed: true),
            MakeEval(p2.Id.Value, passed: false)
        };

        var handler = CreatePolicyHandler([p1, p2], evals);
        var result = await handler.Handle(
            new GetPlatformPolicyComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Distribution.EnforcedCount.Should().Be(1);
        result.Value.Distribution.FailingCount.Should().Be(1);
    }

    [Fact]
    public void AJ3_MapGovernanceType_PromotionGate_Mandatory()
        => GetPlatformPolicyComplianceReport.Handler.MapGovernanceType(PolicyDefinitionType.PromotionGate)
            .Should().Be(GetPlatformPolicyComplianceReport.GovernancePolicyType.Mandatory);

    [Fact]
    public void AJ3_MapGovernanceType_ComplianceCheck_Mandatory()
        => GetPlatformPolicyComplianceReport.Handler.MapGovernanceType(PolicyDefinitionType.ComplianceCheck)
            .Should().Be(GetPlatformPolicyComplianceReport.GovernancePolicyType.Mandatory);

    [Fact]
    public void AJ3_MapGovernanceType_AccessControl_Advisory()
        => GetPlatformPolicyComplianceReport.Handler.MapGovernanceType(PolicyDefinitionType.AccessControl)
            .Should().Be(GetPlatformPolicyComplianceReport.GovernancePolicyType.Advisory);

    [Fact]
    public void AJ3_MapGovernanceType_FreezeWindow_Advisory()
        => GetPlatformPolicyComplianceReport.Handler.MapGovernanceType(PolicyDefinitionType.FreezeWindow)
            .Should().Be(GetPlatformPolicyComplianceReport.GovernancePolicyType.Advisory);

    [Fact]
    public void AJ3_MapGovernanceType_AlertThreshold_Informational()
        => GetPlatformPolicyComplianceReport.Handler.MapGovernanceType(PolicyDefinitionType.AlertThreshold)
            .Should().Be(GetPlatformPolicyComplianceReport.GovernancePolicyType.Informational);

    [Fact]
    public void AJ3_ClassifyTier_Score95_ReturnsEnforced()
        => GetPlatformPolicyComplianceReport.Handler.ClassifyTier(95m).Should()
            .Be(GetPlatformPolicyComplianceReport.PolicyComplianceTier.Enforced);

    [Fact]
    public void AJ3_ClassifyTier_Score949_ReturnsPartial()
        => GetPlatformPolicyComplianceReport.Handler.ClassifyTier(94.9m).Should()
            .Be(GetPlatformPolicyComplianceReport.PolicyComplianceTier.Partial);

    [Fact]
    public void AJ3_ClassifyTier_Score75_ReturnsPartial()
        => GetPlatformPolicyComplianceReport.Handler.ClassifyTier(75m).Should()
            .Be(GetPlatformPolicyComplianceReport.PolicyComplianceTier.Partial);

    [Fact]
    public void AJ3_ClassifyTier_Score749_ReturnsAtRisk()
        => GetPlatformPolicyComplianceReport.Handler.ClassifyTier(74.9m).Should()
            .Be(GetPlatformPolicyComplianceReport.PolicyComplianceTier.AtRisk);

    [Fact]
    public void AJ3_ClassifyTier_Score50_ReturnsAtRisk()
        => GetPlatformPolicyComplianceReport.Handler.ClassifyTier(50m).Should()
            .Be(GetPlatformPolicyComplianceReport.PolicyComplianceTier.AtRisk);

    [Fact]
    public void AJ3_ClassifyTier_Score499_ReturnsFailing()
        => GetPlatformPolicyComplianceReport.Handler.ClassifyTier(49.9m).Should()
            .Be(GetPlatformPolicyComplianceReport.PolicyComplianceTier.Failing);

    [Fact]
    public async Task AJ3_Validator_EmptyTenantId_IsInvalid()
    {
        var validator = new GetPlatformPolicyComplianceReport.Validator();
        var result = await validator.ValidateAsync(new GetPlatformPolicyComplianceReport.Query(string.Empty));
        result.IsValid.Should().BeFalse();
    }
}
