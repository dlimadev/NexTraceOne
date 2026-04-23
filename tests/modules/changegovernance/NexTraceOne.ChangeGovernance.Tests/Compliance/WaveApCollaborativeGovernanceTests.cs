using System.Linq;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetApprovalWorkflowReport;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetPeerReviewCoverageReport;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetGovernanceEscalationReport;

namespace NexTraceOne.ChangeGovernance.Tests.Compliance;

/// <summary>
/// Testes unitários para Wave AP — Collaborative Governance &amp; Workflow Automation.
/// AP.1: GetApprovalWorkflowReport      (~14 testes)
/// AP.2: GetPeerReviewCoverageReport    (~14 testes)
/// AP.3: GetGovernanceEscalationReport  (~15 testes)
/// </summary>
public sealed class WaveApCollaborativeGovernanceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-ap-test";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AP.1 — GetApprovalWorkflowReport
    // ═══════════════════════════════════════════════════════════════════════

    private static IApprovalWorkflowReader.ApprovalEnvironmentEntry MakeApprovalEntry(
        string environment = "production",
        string approvalType = "ManualApproval",
        int totalApprovals = 100,
        decimal avgApprovalTimeHours = 2m,
        decimal slaComplianceRate = 0.95m,
        decimal autoApprovalRate = 0.20m,
        decimal rejectionRate = 0.05m,
        int pendingCount = 3,
        IReadOnlyList<IApprovalWorkflowReader.ApproverBacklog>? backlogs = null)
        => new(
            Environment: environment,
            ApprovalType: approvalType,
            TotalApprovals: totalApprovals,
            AvgApprovalTimeHours: avgApprovalTimeHours,
            SlaComplianceRate: slaComplianceRate,
            AutoApprovalRate: autoApprovalRate,
            RejectionRate: rejectionRate,
            PendingCount: pendingCount,
            ApproverBacklogs: backlogs ?? []);

    private static GetApprovalWorkflowReport.Handler CreateApprovalHandler(
        IReadOnlyList<IApprovalWorkflowReader.ApprovalEnvironmentEntry> entries)
    {
        var reader = Substitute.For<IApprovalWorkflowReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(entries);
        return new GetApprovalWorkflowReport.Handler(reader, CreateClock());
    }

    [Fact]
    public async Task AP1_AvgTime_LessThan4h_Returns_EfficientTier()
    {
        var entries = new[] { MakeApprovalEntry(avgApprovalTimeHours: 3m) };
        var handler = CreateApprovalHandler(entries);
        var result = await handler.Handle(
            new GetApprovalWorkflowReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetApprovalWorkflowReport.ApprovalTier.Efficient);
    }

    [Fact]
    public async Task AP1_AvgTime_8h_Returns_NormalTier()
    {
        var entries = new[] { MakeApprovalEntry(avgApprovalTimeHours: 8m) };
        var handler = CreateApprovalHandler(entries);
        var result = await handler.Handle(
            new GetApprovalWorkflowReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetApprovalWorkflowReport.ApprovalTier.Normal);
    }

    [Fact]
    public async Task AP1_AvgTime_24h_Returns_DelayedTier()
    {
        var entries = new[] { MakeApprovalEntry(avgApprovalTimeHours: 24m) };
        var handler = CreateApprovalHandler(entries);
        var result = await handler.Handle(
            new GetApprovalWorkflowReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetApprovalWorkflowReport.ApprovalTier.Delayed);
    }

    [Fact]
    public async Task AP1_AvgTime_60h_Returns_BlockedTier()
    {
        var entries = new[] { MakeApprovalEntry(avgApprovalTimeHours: 60m) };
        var handler = CreateApprovalHandler(entries);
        var result = await handler.Handle(
            new GetApprovalWorkflowReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetApprovalWorkflowReport.ApprovalTier.Blocked);
    }

    [Fact]
    public async Task AP1_EmptyEnvironments_Returns_HealthScore100_PendingZero()
    {
        var handler = CreateApprovalHandler([]);
        var result = await handler.Handle(
            new GetApprovalWorkflowReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantApprovalHealthScore.Should().Be(100m);
        result.Value.TotalPendingApprovals.Should().Be(0);
        result.Value.Tier.Should().Be(GetApprovalWorkflowReport.ApprovalTier.Efficient);
    }

    [Fact]
    public async Task AP1_BottleneckApprovers_TopFiveByPendingCount()
    {
        var backlogs = Enumerable.Range(1, 7)
            .Select(i => new IApprovalWorkflowReader.ApproverBacklog($"approver-{i}", $"User {i}", i * 10))
            .ToList();
        var entries = new[] { MakeApprovalEntry(backlogs: backlogs) };
        var handler = CreateApprovalHandler(entries);
        var result = await handler.Handle(
            new GetApprovalWorkflowReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BottleneckApprovers.Should().HaveCount(5);
        result.Value.BottleneckApprovers.First().PendingCount.Should().Be(70);
        result.Value.BottleneckApprovers.Last().PendingCount.Should().Be(30);
    }

    [Fact]
    public async Task AP1_ProductionEnvironments_WeightedTwiceInHealthScore()
    {
        // Production: SlaComplianceRate = 1.0 (100%)
        // Non-Production: SlaComplianceRate = 0.0 (0%)
        // Weighted: (100*2 + 0*1) / (2+1) ≈ 66.7
        var entries = new[]
        {
            MakeApprovalEntry("production", slaComplianceRate: 1.0m),
            MakeApprovalEntry("staging", slaComplianceRate: 0.0m)
        };
        var handler = CreateApprovalHandler(entries);
        var result = await handler.Handle(
            new GetApprovalWorkflowReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantApprovalHealthScore.Should().BeApproximately(66.7m, 0.5m);
    }

    [Fact]
    public async Task AP1_QueryValidation_EmptyTenantId_IsInvalid()
    {
        var validator = new GetApprovalWorkflowReport.Validator();
        var validationResult = await validator.ValidateAsync(
            new GetApprovalWorkflowReport.Query(string.Empty));
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AP1_QueryValidation_LookbackDays_Below7_IsInvalid()
    {
        var validator = new GetApprovalWorkflowReport.Validator();
        var validationResult = await validator.ValidateAsync(
            new GetApprovalWorkflowReport.Query(TenantId, LookbackDays: 6));
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AP1_QueryValidation_LookbackDays_Above90_IsInvalid()
    {
        var validator = new GetApprovalWorkflowReport.Validator();
        var validationResult = await validator.ValidateAsync(
            new GetApprovalWorkflowReport.Query(TenantId, LookbackDays: 91));
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AP1_TenantApprovalHealthScore_IsWeightedAvgSlaComplianceRate()
    {
        // Single prod environment: SLA = 80% → score = 80
        var entries = new[] { MakeApprovalEntry("prod-eu", slaComplianceRate: 0.80m) };
        var handler = CreateApprovalHandler(entries);
        var result = await handler.Handle(
            new GetApprovalWorkflowReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantApprovalHealthScore.Should().Be(80m);
    }

    [Fact]
    public async Task AP1_ApprovalEfficiencyIndex_ComputedCorrectly()
    {
        // env1: SLA=0.9, 100 approvals → contributes 90
        // env2: SLA=0.5, 100 approvals → contributes 50
        // Efficiency = (90+50)/200 * 100 = 70%
        var entries = new[]
        {
            MakeApprovalEntry("prod", totalApprovals: 100, slaComplianceRate: 0.90m),
            MakeApprovalEntry("staging", totalApprovals: 100, slaComplianceRate: 0.50m)
        };
        var handler = CreateApprovalHandler(entries);
        var result = await handler.Handle(
            new GetApprovalWorkflowReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ApprovalEfficiencyIndex.Should().BeApproximately(70m, 0.1m);
    }

    [Fact]
    public async Task AP1_AutoApprovalRate_ComputedAsWeightedAvg()
    {
        var entries = new[]
        {
            MakeApprovalEntry("production", autoApprovalRate: 0.40m),
            MakeApprovalEntry("staging", autoApprovalRate: 0.20m)
        };
        var handler = CreateApprovalHandler(entries);
        var result = await handler.Handle(
            new GetApprovalWorkflowReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Prod weight=2, staging weight=1 → (40*2 + 20*1) / 3 = 33.3%
        result.Value!.AutoApprovalRatePct.Should().BeApproximately(33.3m, 0.5m);
    }

    [Fact]
    public async Task AP1_EnvironmentFilter_OnlyReturnsMatchingEnvironment()
    {
        var entries = new[]
        {
            MakeApprovalEntry("production", avgApprovalTimeHours: 2m),
            MakeApprovalEntry("staging", avgApprovalTimeHours: 20m)
        };
        var handler = CreateApprovalHandler(entries);
        var result = await handler.Handle(
            new GetApprovalWorkflowReport.Query(TenantId, EnvironmentFilter: "production"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Environments.Should().HaveCount(1);
        result.Value.Environments.Single().EnvironmentName.Should().Be("production");
        result.Value.Tier.Should().Be(GetApprovalWorkflowReport.ApprovalTier.Efficient);
    }

    [Fact]
    public void AP1_ClassifyTier_Boundary_4h_Returns_Normal()
        => GetApprovalWorkflowReport.Handler.ClassifyTier(4m)
            .Should().Be(GetApprovalWorkflowReport.ApprovalTier.Normal);

    // ═══════════════════════════════════════════════════════════════════════
    // AP.2 — GetPeerReviewCoverageReport
    // ═══════════════════════════════════════════════════════════════════════

    private static IPeerReviewCoverageReader.ChangeReviewEntry MakeChange(
        string changeId = "chg-1",
        string serviceName = "svc-a",
        string teamName = "team-alpha",
        bool hasPeerReview = true,
        int blastRadiusScore = 10,
        int confidenceScore = 80)
        => new(
            ChangeId: changeId,
            ServiceName: serviceName,
            TeamName: teamName,
            HasPeerReview: hasPeerReview,
            ReviewerCount: hasPeerReview ? 1 : 0,
            BlastRadiusScore: blastRadiusScore,
            ConfidenceScore: confidenceScore,
            ReviewerIds: hasPeerReview ? ["reviewer-1"] : []);

    private static IPeerReviewCoverageReader.ContractChangeEntry MakeContractChange(
        string contractId = "ctr-1",
        string contractName = "api-svc",
        bool hasReview = true,
        bool isBreaking = false)
        => new(ContractId: contractId, ContractName: contractName, HasReview: hasReview, IsBreaking: isBreaking);

    private static GetPeerReviewCoverageReport.Handler CreatePeerReviewHandler(
        IReadOnlyList<IPeerReviewCoverageReader.ChangeReviewEntry> changes,
        IReadOnlyList<IPeerReviewCoverageReader.ContractChangeEntry>? contractChanges = null,
        IReadOnlyList<IPeerReviewCoverageReader.ReviewBacklogEntry>? backlogs = null)
    {
        var reader = Substitute.For<IPeerReviewCoverageReader>();
        reader.GetByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new IPeerReviewCoverageReader.PeerReviewTenantData(
                Changes: changes,
                ContractChanges: contractChanges ?? [],
                ReviewBacklogs: backlogs ?? []));
        return new GetPeerReviewCoverageReport.Handler(reader, CreateClock());
    }

    [Fact]
    public async Task AP2_AllReviewed_Returns_FullTier()
    {
        var changes = Enumerable.Range(1, 20)
            .Select(i => MakeChange($"chg-{i}", hasPeerReview: true))
            .ToList();
        var handler = CreatePeerReviewHandler(changes);
        var result = await handler.Handle(
            new GetPeerReviewCoverageReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReviewCoverageRatePct.Should().Be(100m);
        result.Value.Tier.Should().Be(GetPeerReviewCoverageReport.ReviewCompletionTier.Full);
    }

    [Fact]
    public async Task AP2_80PercentReviewed_Returns_GoodTier()
    {
        var reviewed = Enumerable.Range(1, 8).Select(i => MakeChange($"r{i}", hasPeerReview: true));
        var unreviewed = Enumerable.Range(1, 2).Select(i => MakeChange($"u{i}", hasPeerReview: false));
        var handler = CreatePeerReviewHandler([.. reviewed, .. unreviewed]);
        var result = await handler.Handle(
            new GetPeerReviewCoverageReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReviewCoverageRatePct.Should().Be(80m);
        result.Value.Tier.Should().Be(GetPeerReviewCoverageReport.ReviewCompletionTier.Good);
    }

    [Fact]
    public async Task AP2_60PercentReviewed_Returns_PartialTier()
    {
        var reviewed = Enumerable.Range(1, 6).Select(i => MakeChange($"r{i}", hasPeerReview: true));
        var unreviewed = Enumerable.Range(1, 4).Select(i => MakeChange($"u{i}", hasPeerReview: false));
        var handler = CreatePeerReviewHandler([.. reviewed, .. unreviewed]);
        var result = await handler.Handle(
            new GetPeerReviewCoverageReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReviewCoverageRatePct.Should().Be(60m);
        result.Value.Tier.Should().Be(GetPeerReviewCoverageReport.ReviewCompletionTier.Partial);
    }

    [Fact]
    public async Task AP2_40PercentReviewed_Returns_AtRiskTier()
    {
        var reviewed = Enumerable.Range(1, 4).Select(i => MakeChange($"r{i}", hasPeerReview: true));
        var unreviewed = Enumerable.Range(1, 6).Select(i => MakeChange($"u{i}", hasPeerReview: false));
        var handler = CreatePeerReviewHandler([.. reviewed, .. unreviewed]);
        var result = await handler.Handle(
            new GetPeerReviewCoverageReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReviewCoverageRatePct.Should().Be(40m);
        result.Value.Tier.Should().Be(GetPeerReviewCoverageReport.ReviewCompletionTier.AtRisk);
    }

    [Fact]
    public async Task AP2_EmptyChanges_Returns_FullTier_Score100()
    {
        var handler = CreatePeerReviewHandler([]);
        var result = await handler.Handle(
            new GetPeerReviewCoverageReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetPeerReviewCoverageReport.ReviewCompletionTier.Full);
        result.Value.TenantPeerReviewScore.Should().Be(100m);
    }

    [Fact]
    public async Task AP2_UnreviewedHighRiskChanges_ContainsCorrectItems()
    {
        var changes = new[]
        {
            MakeChange("high-reviewed", hasPeerReview: true, blastRadiusScore: 70),
            MakeChange("high-unreviewed", hasPeerReview: false, blastRadiusScore: 70),
            MakeChange("low-unreviewed", hasPeerReview: false, blastRadiusScore: 10)
        };
        var handler = CreatePeerReviewHandler(changes);
        var result = await handler.Handle(
            new GetPeerReviewCoverageReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UnreviewedHighRiskChanges.Should().HaveCount(1);
        result.Value.UnreviewedHighRiskChanges.Single().ChangeId.Should().Be("high-unreviewed");
    }

    [Fact]
    public async Task AP2_ReviewThrottleRisk_TrueWhenBacklogGreaterThan5()
    {
        var backlogs = Enumerable.Range(1, 6)
            .Select(i => new IPeerReviewCoverageReader.ReviewBacklogEntry($"chg-{i}", $"svc-{i}", i * 2))
            .ToList();
        // Include at least one change so the handler doesn't short-circuit via empty check
        var changes = new[] { MakeChange("c-anchor", hasPeerReview: true) };
        var handler = CreatePeerReviewHandler(changes, backlogs: backlogs);
        var result = await handler.Handle(
            new GetPeerReviewCoverageReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReviewThrottleRisk.Should().BeTrue();
    }

    [Fact]
    public async Task AP2_ContractChangeReviewRate_ComputedCorrectly()
    {
        var contractChanges = new[]
        {
            MakeContractChange("c1", hasReview: true),
            MakeContractChange("c2", hasReview: true),
            MakeContractChange("c3", hasReview: false)
        };
        var handler = CreatePeerReviewHandler([], contractChanges);
        var result = await handler.Handle(
            new GetPeerReviewCoverageReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ContractChangeReviewRatePct.Should().BeApproximately(66.7m, 0.5m);
    }

    [Fact]
    public async Task AP2_BreakingContractChangesWithoutReview_Identified()
    {
        var contractChanges = new[]
        {
            MakeContractChange("c1", hasReview: false, isBreaking: true),
            MakeContractChange("c2", hasReview: true, isBreaking: true),
            MakeContractChange("c3", hasReview: false, isBreaking: false)
        };
        var handler = CreatePeerReviewHandler([], contractChanges);
        var result = await handler.Handle(
            new GetPeerReviewCoverageReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BreakingContractChangesWithoutReview.Should().HaveCount(1);
        result.Value.BreakingContractChangesWithoutReview.Single().ContractId.Should().Be("c1");
    }

    [Fact]
    public async Task AP2_TenantPeerReviewScore_WeightedCorrectly()
    {
        // ReviewCoverageRate = 100%, HighRiskReviewRate = 100%
        // Score = 100*0.5 + 100*0.5 = 100
        var changes = Enumerable.Range(1, 5)
            .Select(i => MakeChange($"c{i}", hasPeerReview: true, blastRadiusScore: 60))
            .ToList();
        var handler = CreatePeerReviewHandler(changes);
        var result = await handler.Handle(
            new GetPeerReviewCoverageReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantPeerReviewScore.Should().Be(100m);
    }

    [Fact]
    public async Task AP2_TeamFilter_AppliesCorrectly()
    {
        var changes = new[]
        {
            MakeChange("c1", teamName: "team-alpha", hasPeerReview: true),
            MakeChange("c2", teamName: "team-beta", hasPeerReview: false)
        };
        var handler = CreatePeerReviewHandler(changes);
        var result = await handler.Handle(
            new GetPeerReviewCoverageReport.Query(TenantId, TeamFilter: "team-alpha"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Only team-alpha: 1 reviewed / 1 total = 100%
        result.Value!.ReviewCoverageRatePct.Should().Be(100m);
    }

    [Fact]
    public async Task AP2_QueryValidation_EmptyTenantId_IsInvalid()
    {
        var validator = new GetPeerReviewCoverageReport.Validator();
        var validationResult = await validator.ValidateAsync(
            new GetPeerReviewCoverageReport.Query(string.Empty));
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AP2_QueryValidation_LookbackDays_OutOfRange_IsInvalid()
    {
        var validator = new GetPeerReviewCoverageReport.Validator();
        var r1 = await validator.ValidateAsync(new GetPeerReviewCoverageReport.Query(TenantId, LookbackDays: 6));
        var r2 = await validator.ValidateAsync(new GetPeerReviewCoverageReport.Query(TenantId, LookbackDays: 91));
        r1.IsValid.Should().BeFalse();
        r2.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AP.3 — GetGovernanceEscalationReport
    // ═══════════════════════════════════════════════════════════════════════

    private static IGovernanceEscalationReader.BreakGlassEventEntry MakeBreakGlass(
        string userId = "user-1",
        string userName = "Alice",
        string environment = "production",
        bool isProduction = true)
        => new(
            EventId: Guid.NewGuid().ToString(),
            UserId: userId,
            UserName: userName,
            Environment: environment,
            OccurredAt: FixedNow.AddDays(-1),
            ResolvedAt: null,
            IsProduction: isProduction);

    private static IGovernanceEscalationReader.JitAccessEntry MakeJit(
        string userId = "user-1",
        bool isApproved = true,
        bool isRejected = false,
        bool isAutoApproved = false,
        DateTimeOffset? lastUsedAt = null)
        => new(
            RequestId: Guid.NewGuid().ToString(),
            UserId: userId,
            UserName: $"User {userId}",
            IsApproved: isApproved,
            IsRejected: isRejected,
            IsAutoApproved: isAutoApproved,
            GrantedAt: FixedNow.AddDays(-2),
            ExpiresAt: FixedNow.AddDays(1),
            LastUsedAt: lastUsedAt);

    private static GetGovernanceEscalationReport.Handler CreateEscalationHandler(
        IReadOnlyList<IGovernanceEscalationReader.BreakGlassEventEntry> breakGlassEvents,
        IReadOnlyList<IGovernanceEscalationReader.JitAccessEntry>? jitRequests = null,
        decimal? previousPeriodCount = null)
    {
        var reader = Substitute.For<IGovernanceEscalationReader>();
        reader.GetByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new IGovernanceEscalationReader.GovernanceEscalationData(
                BreakGlassEvents: breakGlassEvents,
                JitAccessRequests: jitRequests ?? [],
                PreviousPeriodBreakGlassCount: previousPeriodCount));
        return new GetGovernanceEscalationReport.Handler(reader, CreateClock());
    }

    [Fact]
    public async Task AP3_Zero_BreakGlass_Returns_LowTier()
    {
        var handler = CreateEscalationHandler([]);
        var result = await handler.Handle(
            new GetGovernanceEscalationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetGovernanceEscalationReport.EscalationRiskTier.Low);
        result.Value.BreakGlassCount.Should().Be(0);
    }

    [Fact]
    public async Task AP3_Five_BreakGlass_Returns_MediumTier()
    {
        var events = Enumerable.Range(1, 5).Select(_ => MakeBreakGlass()).ToList();
        var handler = CreateEscalationHandler(events);
        var result = await handler.Handle(
            new GetGovernanceEscalationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetGovernanceEscalationReport.EscalationRiskTier.Medium);
    }

    [Fact]
    public async Task AP3_Fifteen_BreakGlass_Returns_HighTier()
    {
        var events = Enumerable.Range(1, 15).Select(_ => MakeBreakGlass()).ToList();
        var handler = CreateEscalationHandler(events);
        var result = await handler.Handle(
            new GetGovernanceEscalationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetGovernanceEscalationReport.EscalationRiskTier.High);
    }

    [Fact]
    public async Task AP3_TwentyFive_BreakGlass_Returns_CriticalTier()
    {
        var events = Enumerable.Range(1, 25).Select(_ => MakeBreakGlass()).ToList();
        var handler = CreateEscalationHandler(events);
        var result = await handler.Handle(
            new GetGovernanceEscalationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetGovernanceEscalationReport.EscalationRiskTier.Critical);
    }

    [Fact]
    public async Task AP3_TopEscalatingUsers_CorrectlyIdentified()
    {
        // user-a: 3 events, user-b: 2 events, user-c: 1 event
        var events = new List<IGovernanceEscalationReader.BreakGlassEventEntry>
        {
            MakeBreakGlass("user-a", "Alice"), MakeBreakGlass("user-a", "Alice"), MakeBreakGlass("user-a", "Alice"),
            MakeBreakGlass("user-b", "Bob"), MakeBreakGlass("user-b", "Bob"),
            MakeBreakGlass("user-c", "Carol")
        };
        var handler = CreateEscalationHandler(events);
        var result = await handler.Handle(
            new GetGovernanceEscalationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TopEscalatingUsers.Should().HaveCount(3);
        result.Value.TopEscalatingUsers.First().UserId.Should().Be("user-a");
        result.Value.TopEscalatingUsers.First().BreakGlassCount.Should().Be(3);
    }

    [Fact]
    public async Task AP3_BreakGlassSurge_Flag_WhenCountGreaterThan150PctOfPrevious()
    {
        // 10 current, 5 previous → 10 > 5*1.5 = 7.5 → Surge
        var events = Enumerable.Range(1, 10).Select(_ => MakeBreakGlass()).ToList();
        var handler = CreateEscalationHandler(events, previousPeriodCount: 5);
        var result = await handler.Handle(
            new GetGovernanceEscalationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EscalationPatternFlags.Should().Contain("BreakGlassSurge");
    }

    [Fact]
    public async Task AP3_ProductionOnlyEscalations_Flag_WhenAllEventsAreProduction()
    {
        var events = Enumerable.Range(1, 5)
            .Select(_ => MakeBreakGlass(environment: "production", isProduction: true))
            .ToList();
        var handler = CreateEscalationHandler(events);
        var result = await handler.Handle(
            new GetGovernanceEscalationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EscalationPatternFlags.Should().Contain("ProductionOnlyEscalations");
    }

    [Fact]
    public async Task AP3_JitAbuse_Flag_WhenAutoApprovedRateAbove80Percent()
    {
        var jits = Enumerable.Range(1, 10)
            .Select(i => MakeJit(isAutoApproved: i <= 9))
            .ToList();
        var handler = CreateEscalationHandler([], jitRequests: jits);
        var result = await handler.Handle(
            new GetGovernanceEscalationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EscalationPatternFlags.Should().Contain("JitAbuse");
    }

    [Fact]
    public async Task AP3_UnusedPrivilegedAccess_Flag_WhenNeverUsedCountAbove5()
    {
        var jits = Enumerable.Range(1, 6)
            .Select(_ => MakeJit(isApproved: true, isRejected: false, lastUsedAt: null))
            .ToList();
        var handler = CreateEscalationHandler([], jitRequests: jits);
        var result = await handler.Handle(
            new GetGovernanceEscalationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EscalationPatternFlags.Should().Contain("UnusedPrivilegedAccess");
        result.Value.JitNeverUsedCount.Should().Be(6);
    }

    [Fact]
    public async Task AP3_JitApprovedRate_ComputedCorrectly()
    {
        // 7 approved, 3 not approved = 70%
        var jits = new List<IGovernanceEscalationReader.JitAccessEntry>();
        for (int i = 0; i < 7; i++) jits.Add(MakeJit(isApproved: true));
        for (int i = 0; i < 3; i++) jits.Add(MakeJit(isApproved: false));

        var handler = CreateEscalationHandler([], jitRequests: jits);
        var result = await handler.Handle(
            new GetGovernanceEscalationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.JitApprovedRatePct.Should().Be(70m);
    }

    [Fact]
    public async Task AP3_TenantEscalationRiskScore_CappedAt100()
    {
        // 25 break glass (prod) → 25*5 + 25*3 = 200 → capped at 100
        var events = Enumerable.Range(1, 25)
            .Select(_ => MakeBreakGlass(isProduction: true))
            .ToList();
        var handler = CreateEscalationHandler(events);
        var result = await handler.Handle(
            new GetGovernanceEscalationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantEscalationRiskScore.Should().Be(100m);
    }

    [Fact]
    public async Task AP3_EmptyData_Returns_LowTier_Score0()
    {
        var handler = CreateEscalationHandler([]);
        var result = await handler.Handle(
            new GetGovernanceEscalationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetGovernanceEscalationReport.EscalationRiskTier.Low);
        result.Value.TenantEscalationRiskScore.Should().Be(0m);
    }

    [Fact]
    public async Task AP3_PreviousPeriodBreakGlassCount_NullHandledGracefully()
    {
        var events = Enumerable.Range(1, 5).Select(_ => MakeBreakGlass()).ToList();
        var handler = CreateEscalationHandler(events, previousPeriodCount: null);
        var result = await handler.Handle(
            new GetGovernanceEscalationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PreviousPeriodBreakGlassCount.Should().BeNull();
        result.Value.EscalationPatternFlags.Should().NotContain("BreakGlassSurge");
    }

    [Fact]
    public async Task AP3_JitNeverUsedCount_ComputedCorrectly()
    {
        var jits = new[]
        {
            MakeJit(isApproved: true, isRejected: false, lastUsedAt: null),       // never used
            MakeJit(isApproved: true, isRejected: false, lastUsedAt: FixedNow),   // used
            MakeJit(isApproved: false, isRejected: true, lastUsedAt: null),       // rejected, not counted
        };
        var handler = CreateEscalationHandler([], jitRequests: jits);
        var result = await handler.Handle(
            new GetGovernanceEscalationReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.JitNeverUsedCount.Should().Be(1);
    }

    [Fact]
    public async Task AP3_QueryValidation_EmptyTenantId_IsInvalid()
    {
        var validator = new GetGovernanceEscalationReport.Validator();
        var validationResult = await validator.ValidateAsync(
            new GetGovernanceEscalationReport.Query(string.Empty));
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AP3_QueryValidation_LookbackDays_OutOfRange_IsInvalid()
    {
        var validator = new GetGovernanceEscalationReport.Validator();
        var r1 = await validator.ValidateAsync(new GetGovernanceEscalationReport.Query(TenantId, LookbackDays: 6));
        var r2 = await validator.ValidateAsync(new GetGovernanceEscalationReport.Query(TenantId, LookbackDays: 91));
        r1.IsValid.Should().BeFalse();
        r2.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AP3_ClassifyTier_Boundary_3_Returns_Medium()
        => GetGovernanceEscalationReport.Handler.ClassifyTier(3)
            .Should().Be(GetGovernanceEscalationReport.EscalationRiskTier.Medium);

    [Fact]
    public void AP3_ClassifyTier_Boundary_10_Returns_High()
        => GetGovernanceEscalationReport.Handler.ClassifyTier(10)
            .Should().Be(GetGovernanceEscalationReport.EscalationRiskTier.High);

    [Fact]
    public void AP3_ClassifyTier_Boundary_20_Returns_Critical()
        => GetGovernanceEscalationReport.Handler.ClassifyTier(20)
            .Should().Be(GetGovernanceEscalationReport.EscalationRiskTier.Critical);
}
