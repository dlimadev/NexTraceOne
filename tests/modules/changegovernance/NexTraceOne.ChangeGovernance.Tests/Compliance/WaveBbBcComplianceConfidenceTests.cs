using System.Linq;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetCrossStandardComplianceGapReport;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetEvidenceCollectionStatusReport;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetRegulatoryChangeImpactReport;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetEvidencePackIntegrityReport;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetMultiDimensionalPromotionConfidenceReport;

namespace NexTraceOne.ChangeGovernance.Tests.Compliance;

/// <summary>
/// Testes unitários para Wave BB — Compliance Automation &amp; Regulatory Reporting
/// e Wave BC — Production Change Confidence.
///
/// BB.1: GetCrossStandardComplianceGapReport        (~14 testes)
/// BB.2: GetEvidenceCollectionStatusReport          (~14 testes)
/// BB.3: GetRegulatoryChangeImpactReport            (~10 testes)
/// BC.2: GetEvidencePackIntegrityReport             (~14 testes)
/// BC.3: GetMultiDimensionalPromotionConfidenceReport (~14 testes)
/// </summary>
public sealed class WaveBbBcComplianceConfidenceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-bb-bc-test";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BB.1 — GetCrossStandardComplianceGapReport
    // ═══════════════════════════════════════════════════════════════════════

    private static ICrossStandardComplianceGapReader.ComplianceGapEntry MakeGapEntry(
        string gapId = "gap-1",
        string gapName = "Access Control Gap",
        string gapType = "Technical",
        IReadOnlyList<string>? affectedStandards = null,
        string serviceTier = "Critical",
        decimal impactScore = 6m,
        int remediationComplexity = 3,
        bool isRemediated = false)
        => new(
            GapId: gapId,
            GapName: gapName,
            GapType: gapType,
            AffectedStandards: affectedStandards ?? ["GDPR", "HIPAA"],
            AffectedServiceIds: ["svc-1"],
            ServiceTier: serviceTier,
            ImpactScore: impactScore,
            RemediationComplexity: remediationComplexity,
            IsRemediated: isRemediated);

    private static GetCrossStandardComplianceGapReport.Handler CreateCrossGapHandler(
        IReadOnlyList<ICrossStandardComplianceGapReader.ComplianceGapEntry> gaps)
    {
        var reader = Substitute.For<ICrossStandardComplianceGapReader>();
        reader.ListGapsByTenantAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(gaps);
        return new GetCrossStandardComplianceGapReport.Handler(reader, CreateClock());
    }

    [Fact]
    public async Task BB1_EmptyGaps_Returns_MinimalTier_Score100()
    {
        var handler = CreateCrossGapHandler([]);
        var result = await handler.Handle(
            new GetCrossStandardComplianceGapReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetCrossStandardComplianceGapReport.CrossStandardGapTier.Minimal);
        result.Value.TenantCrossComplianceScore.Should().Be(100m);
        result.Value.TotalGaps.Should().Be(0);
    }

    [Fact]
    public async Task BB1_FewOpenCrossGaps_Returns_MinimalTier()
    {
        var gaps = Enumerable.Range(1, 3)
            .Select(i => MakeGapEntry($"gap-{i}", affectedStandards: ["GDPR", "HIPAA"], isRemediated: false))
            .ToList();
        var handler = CreateCrossGapHandler(gaps);
        var result = await handler.Handle(
            new GetCrossStandardComplianceGapReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetCrossStandardComplianceGapReport.CrossStandardGapTier.Minimal);
    }

    [Fact]
    public async Task BB1_ManyOpenCrossGaps_Returns_CriticalTier()
    {
        var gaps = Enumerable.Range(1, 35)
            .Select(i => MakeGapEntry($"gap-{i}", affectedStandards: ["GDPR", "HIPAA", "PCI-DSS"], isRemediated: false))
            .ToList();
        var handler = CreateCrossGapHandler(gaps);
        var result = await handler.Handle(
            new GetCrossStandardComplianceGapReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetCrossStandardComplianceGapReport.CrossStandardGapTier.Critical);
    }

    [Fact]
    public async Task BB1_AllRemediated_Returns_Score100_MinimalTier()
    {
        var gaps = Enumerable.Range(1, 20)
            .Select(i => MakeGapEntry($"gap-{i}", affectedStandards: ["GDPR", "HIPAA"], isRemediated: true))
            .ToList();
        var handler = CreateCrossGapHandler(gaps);
        var result = await handler.Handle(
            new GetCrossStandardComplianceGapReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantCrossComplianceScore.Should().Be(100m);
        result.Value.Tier.Should().Be(GetCrossStandardComplianceGapReport.CrossStandardGapTier.Minimal);
    }

    [Fact]
    public async Task BB1_SingleStandardGap_ExcludedFromCrossStandardGaps()
    {
        var gaps = new[]
        {
            MakeGapEntry("gap-1", affectedStandards: ["GDPR"]), // single-standard — excluded
            MakeGapEntry("gap-2", affectedStandards: ["GDPR", "HIPAA"]) // cross-standard
        };
        var handler = CreateCrossGapHandler(gaps);
        var result = await handler.Handle(
            new GetCrossStandardComplianceGapReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CrossStandardGapsCount.Should().Be(1);
    }

    [Fact]
    public async Task BB1_PriorityList_LimitedToTopPriorityCount()
    {
        var gaps = Enumerable.Range(1, 20)
            .Select(i => MakeGapEntry($"gap-{i}", affectedStandards: ["GDPR", "HIPAA"], remediationComplexity: i))
            .ToList();
        var handler = CreateCrossGapHandler(gaps);
        var result = await handler.Handle(
            new GetCrossStandardComplianceGapReport.Query(TenantId, TopPriorityCount: 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantCompliancePriorityList.Should().HaveCount(5);
    }

    [Fact]
    public async Task BB1_GapMatrix_ContainsAllStandardsForEachGap()
    {
        var gap = MakeGapEntry("gap-1", affectedStandards: ["GDPR", "HIPAA", "PCI-DSS"]);
        var handler = CreateCrossGapHandler([gap]);
        var result = await handler.Handle(
            new GetCrossStandardComplianceGapReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Should have cells for each of the 5 default standards
        result.Value!.CrossStandardGapMatrix.Should().NotBeEmpty();
        result.Value.CrossStandardGapMatrix.Where(c => c.GapId == "gap-1" && c.IsAffected).Should().HaveCount(3);
    }

    [Fact]
    public async Task BB1_CriticalServiceTierWeight_3x()
    {
        // Critical tier × 2 standards = impact 6
        var gap = MakeGapEntry("gap-1", affectedStandards: ["GDPR", "HIPAA"], serviceTier: "Critical");
        var handler = CreateCrossGapHandler([gap]);
        var result = await handler.Handle(
            new GetCrossStandardComplianceGapReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CrossStandardGaps.Should().HaveCount(1);
        result.Value.CrossStandardGaps[0].ImpactScore.Should().Be(6m); // 2 standards × 3 (Critical weight)
    }

    [Fact]
    public async Task BB1_InternalServiceTierWeight_1x()
    {
        // Internal tier × 3 standards = impact 3
        var gap = MakeGapEntry("gap-1", affectedStandards: ["GDPR", "HIPAA", "PCI-DSS"], serviceTier: "Internal");
        var handler = CreateCrossGapHandler([gap]);
        var result = await handler.Handle(
            new GetCrossStandardComplianceGapReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CrossStandardGaps[0].ImpactScore.Should().Be(3m); // 3 standards × 1 (Internal weight)
    }

    [Fact]
    public async Task BB1_EstimatedComplianceLift_NonZeroForOpenGaps()
    {
        var gaps = Enumerable.Range(1, 5)
            .Select(i => MakeGapEntry($"gap-{i}", affectedStandards: ["GDPR", "HIPAA"]))
            .ToList();
        var handler = CreateCrossGapHandler(gaps);
        var result = await handler.Handle(
            new GetCrossStandardComplianceGapReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EstimatedComplianceLift.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task BB1_PartialTierAt10OpenGaps()
    {
        var gaps = Enumerable.Range(1, 10)
            .Select(i => MakeGapEntry($"gap-{i}", affectedStandards: ["GDPR", "HIPAA"], isRemediated: false))
            .ToList();
        var handler = CreateCrossGapHandler(gaps);
        var result = await handler.Handle(
            new GetCrossStandardComplianceGapReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetCrossStandardComplianceGapReport.CrossStandardGapTier.Partial);
    }

    [Fact]
    public async Task BB1_SignificantTierAt20OpenGaps()
    {
        var gaps = Enumerable.Range(1, 20)
            .Select(i => MakeGapEntry($"gap-{i}", affectedStandards: ["GDPR", "HIPAA"], isRemediated: false))
            .ToList();
        var handler = CreateCrossGapHandler(gaps);
        var result = await handler.Handle(
            new GetCrossStandardComplianceGapReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetCrossStandardComplianceGapReport.CrossStandardGapTier.Significant);
    }

    [Fact]
    public async Task BB1_RemediatedGaps_CountedCorrectly()
    {
        var gaps = new[]
        {
            MakeGapEntry("gap-1", affectedStandards: ["GDPR", "HIPAA"], isRemediated: true),
            MakeGapEntry("gap-2", affectedStandards: ["GDPR", "HIPAA"], isRemediated: false),
        };
        var handler = CreateCrossGapHandler(gaps);
        var result = await handler.Handle(
            new GetCrossStandardComplianceGapReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RemediatedGaps.Should().Be(1);
        result.Value.TotalGaps.Should().Be(2);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BB.2 — GetEvidenceCollectionStatusReport
    // ═══════════════════════════════════════════════════════════════════════

    private static IEvidenceCollectionStatusReader.EvidenceControlEntry MakeControlEntry(
        string controlId = "ctrl-1",
        string standard = "GDPR",
        bool isCollected = true,
        bool isAutoCollectable = false,
        DateTimeOffset? lastCollectedAt = null)
        => new(
            ControlId: controlId,
            ControlName: $"Control {controlId}",
            Standard: standard,
            IsCollected: isCollected,
            IsAutoCollectable: isAutoCollectable,
            LastCollectedAt: lastCollectedAt ?? FixedNow.AddDays(-10),
            EvidenceType: "Automated");

    private static GetEvidenceCollectionStatusReport.Handler CreateEvidenceHandler(
        IReadOnlyList<IEvidenceCollectionStatusReader.EvidenceControlEntry> controls,
        DateTimeOffset? nextAudit = null)
    {
        var reader = Substitute.For<IEvidenceCollectionStatusReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(controls);
        reader.GetNextAuditDateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(nextAudit);
        return new GetEvidenceCollectionStatusReport.Handler(reader, CreateClock());
    }

    [Fact]
    public async Task BB2_EmptyControls_Returns_ReadyTier()
    {
        var handler = CreateEvidenceHandler([]);
        var result = await handler.Handle(
            new GetEvidenceCollectionStatusReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OverallTier.Should().Be(GetEvidenceCollectionStatusReport.AuditReadinessTier.Ready);
        result.Value.OverallEvidenceCompletenessPct.Should().Be(100m);
    }

    [Fact]
    public async Task BB2_AllCollected_Returns_ReadyTier_100Pct()
    {
        var controls = Enumerable.Range(1, 10)
            .Select(i => MakeControlEntry($"ctrl-{i}", isCollected: true))
            .ToList();
        var handler = CreateEvidenceHandler(controls);
        var result = await handler.Handle(
            new GetEvidenceCollectionStatusReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OverallTier.Should().Be(GetEvidenceCollectionStatusReport.AuditReadinessTier.Ready);
        result.Value.OverallEvidenceCompletenessPct.Should().Be(100m);
    }

    [Fact]
    public async Task BB2_HalfCollected_Returns_NeedsWorkTier()
    {
        var controls = Enumerable.Range(1, 10)
            .Select(i => MakeControlEntry($"ctrl-{i}", isCollected: i <= 5))
            .ToList();
        var handler = CreateEvidenceHandler(controls);
        var result = await handler.Handle(
            new GetEvidenceCollectionStatusReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OverallTier.Should().Be(GetEvidenceCollectionStatusReport.AuditReadinessTier.NeedsWork);
    }

    [Fact]
    public async Task BB2_Below50Pct_Returns_NotReadyTier()
    {
        var controls = Enumerable.Range(1, 10)
            .Select(i => MakeControlEntry($"ctrl-{i}", isCollected: i <= 3))
            .ToList();
        var handler = CreateEvidenceHandler(controls);
        var result = await handler.Handle(
            new GetEvidenceCollectionStatusReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OverallTier.Should().Be(GetEvidenceCollectionStatusReport.AuditReadinessTier.NotReady);
    }

    [Fact]
    public async Task BB2_StaleEvidence_CountedCorrectly()
    {
        // Stale = collected but older than 90 days
        var controls = new[]
        {
            MakeControlEntry("ctrl-1", isCollected: true, lastCollectedAt: FixedNow.AddDays(-100)), // stale
            MakeControlEntry("ctrl-2", isCollected: true, lastCollectedAt: FixedNow.AddDays(-10))   // fresh
        };
        var handler = CreateEvidenceHandler(controls);
        var result = await handler.Handle(
            new GetEvidenceCollectionStatusReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalStaleEvidences.Should().Be(1);
    }

    [Fact]
    public async Task BB2_AutoCollectable_GapCount_SeparatedFromManual()
    {
        var controls = new[]
        {
            MakeControlEntry("ctrl-1", isCollected: false, isAutoCollectable: true),
            MakeControlEntry("ctrl-2", isCollected: false, isAutoCollectable: false),
            MakeControlEntry("ctrl-3", isCollected: true)
        };
        var handler = CreateEvidenceHandler(controls);
        var result = await handler.Handle(
            new GetEvidenceCollectionStatusReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AutoCollectableCount.Should().Be(1);
        result.Value.ManualRequiredCount.Should().Be(1);
    }

    [Fact]
    public async Task BB2_NextAudit_Set_DaysToAuditCalculated()
    {
        var nextAudit = FixedNow.AddDays(14);
        var controls = new[]
        {
            MakeControlEntry("ctrl-1", isCollected: true)
        };
        var handler = CreateEvidenceHandler(controls, nextAudit);
        var result = await handler.Handle(
            new GetEvidenceCollectionStatusReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DaysToAudit.Should().Be(14);
    }

    [Fact]
    public async Task BB2_GroupedByStandard_SeparateSummaries()
    {
        var controls = new[]
        {
            MakeControlEntry("ctrl-1", standard: "GDPR", isCollected: true),
            MakeControlEntry("ctrl-2", standard: "GDPR", isCollected: false),
            MakeControlEntry("ctrl-3", standard: "HIPAA", isCollected: true)
        };
        var handler = CreateEvidenceHandler(controls);
        var result = await handler.Handle(
            new GetEvidenceCollectionStatusReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ByStandard.Should().HaveCount(2);
        result.Value.ByStandard.First(s => s.Standard == "GDPR").EvidenceCompletenessPct.Should().Be(50m);
        result.Value.ByStandard.First(s => s.Standard == "HIPAA").EvidenceCompletenessPct.Should().Be(100m);
    }

    [Fact]
    public async Task BB2_GapItems_ContainOnlyUncollectedControls()
    {
        var controls = new[]
        {
            MakeControlEntry("ctrl-1", isCollected: true),
            MakeControlEntry("ctrl-2", isCollected: false),
            MakeControlEntry("ctrl-3", isCollected: false)
        };
        var handler = CreateEvidenceHandler(controls);
        var result = await handler.Handle(
            new GetEvidenceCollectionStatusReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EvidenceGapsByControl.Should().HaveCount(2);
        result.Value.EvidenceGapsByControl.Should().NotContain(g => g.ControlId == "ctrl-1");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BB.3 — GetRegulatoryChangeImpactReport
    // ═══════════════════════════════════════════════════════════════════════

    private static IRegulatoryChangeImpactReader.ServiceRegulatoryImpactEntry MakeImpactEntry(
        string serviceId = "svc-1",
        string tier = "Critical",
        bool hasExisting = false,
        string effortLevel = "Medium",
        int effortDays = 15)
        => new(
            ServiceId: serviceId,
            ServiceName: $"Service {serviceId}",
            ServiceTier: tier,
            TeamId: "team-1",
            HasExistingControl: hasExisting,
            MitigationPath: hasExisting ? "Update existing control" : "Implement new control",
            EstimatedEffortLevel: effortLevel,
            EstimatedEffortDays: effortDays);

    private static GetRegulatoryChangeImpactReport.Handler CreateRegulatoryHandler(
        IReadOnlyList<IRegulatoryChangeImpactReader.ServiceRegulatoryImpactEntry> entries,
        decimal readinessScore = 75m)
    {
        var reader = Substitute.For<IRegulatoryChangeImpactReader>();
        reader.ListImpactedServicesAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(entries);
        reader.GetTenantRegulatoryReadinessScoreAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(readinessScore);
        return new GetRegulatoryChangeImpactReport.Handler(reader, CreateClock());
    }

    [Fact]
    public async Task BB3_NoImpactedServices_Returns_EmptyReport_WithReadinessScore()
    {
        var handler = CreateRegulatoryHandler([], readinessScore: 90m);
        var result = await handler.Handle(
            new GetRegulatoryChangeImpactReport.Command(TenantId, "GDPR", "Art25", "EU"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ImpactedServicesCount.Should().Be(0);
        result.Value.TenantRegulatoryReadinessScore.Should().Be(90m);
    }

    [Fact]
    public async Task BB3_HighEffortService_Overall_IsHigh()
    {
        var entries = new[] { MakeImpactEntry(effortLevel: "High", effortDays: 60) };
        var handler = CreateRegulatoryHandler(entries);
        var result = await handler.Handle(
            new GetRegulatoryChangeImpactReport.Command(TenantId, "GDPR", "Art25", "EU"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OverallRemediationEffort.Should()
            .Be(GetRegulatoryChangeImpactReport.RemediationEffortLevel.High);
    }

    [Fact]
    public async Task BB3_AllLowEffort_Overall_IsLow()
    {
        var entries = Enumerable.Range(1, 3)
            .Select(i => MakeImpactEntry($"svc-{i}", effortLevel: "Low", effortDays: 2))
            .ToList();
        var handler = CreateRegulatoryHandler(entries);
        var result = await handler.Handle(
            new GetRegulatoryChangeImpactReport.Command(TenantId, "GDPR", "Art25", "EU"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OverallRemediationEffort.Should()
            .Be(GetRegulatoryChangeImpactReport.RemediationEffortLevel.Low);
    }

    [Fact]
    public async Task BB3_ServicesWithExistingControl_Counted()
    {
        var entries = new[]
        {
            MakeImpactEntry("svc-1", hasExisting: true),
            MakeImpactEntry("svc-2", hasExisting: false),
        };
        var handler = CreateRegulatoryHandler(entries);
        var result = await handler.Handle(
            new GetRegulatoryChangeImpactReport.Command(TenantId, "GDPR", "Art25", "EU"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ServicesWithExistingControl.Should().Be(1);
        result.Value.ImpactedServicesCount.Should().Be(2);
    }

    [Fact]
    public async Task BB3_TotalEffortDays_SumOfAllServices()
    {
        var entries = new[]
        {
            MakeImpactEntry("svc-1", effortDays: 10),
            MakeImpactEntry("svc-2", effortDays: 20),
        };
        var handler = CreateRegulatoryHandler(entries);
        var result = await handler.Handle(
            new GetRegulatoryChangeImpactReport.Command(TenantId, "GDPR", "Art25", "EU"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalEstimatedEffortDays.Should().Be(30);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BC.2 — GetEvidencePackIntegrityReport
    // ═══════════════════════════════════════════════════════════════════════

    private static IEvidencePackIntegrityReader.EvidencePackEntry MakePackEntry(
        string packId = "pack-1",
        string releaseId = "release-1",
        bool isProd = false,
        bool isHashValid = true,
        bool isComplete = true,
        bool isConsistent = true,
        bool hasSignature = true,
        bool isSignatureValid = true,
        int itemCount = 5,
        int missingCount = 0)
        => new(
            EvidencePackId: packId,
            ReleaseId: releaseId,
            ServiceId: "svc-1",
            ServiceName: "Service One",
            IsProductionRelease: isProd,
            IsHashValid: isHashValid,
            IsComplete: isComplete,
            IsConsistent: isConsistent,
            HasSignature: hasSignature,
            IsSignatureValid: isSignatureValid,
            CreatedAt: FixedNow.AddHours(-1),
            EvidenceItemCount: itemCount,
            MissingEvidenceCount: missingCount);

    private static GetEvidencePackIntegrityReport.Handler CreateIntegrityHandler(
        IReadOnlyList<IEvidencePackIntegrityReader.EvidencePackEntry> packs)
    {
        var reader = Substitute.For<IEvidencePackIntegrityReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(packs);
        return new GetEvidencePackIntegrityReport.Handler(reader, CreateClock());
    }

    [Fact]
    public async Task BC2_EmptyPacks_Returns_TrustworthyTier()
    {
        var handler = CreateIntegrityHandler([]);
        var result = await handler.Handle(
            new GetEvidencePackIntegrityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OverallTier.Should().Be(GetEvidencePackIntegrityReport.EvidencePackIntegrityTier.Trustworthy);
        result.Value.TenantEvidencePackScore.Should().Be(100m);
    }

    [Fact]
    public async Task BC2_IntactPack_Returns_TrustworthyTier()
    {
        var pack = MakePackEntry(isHashValid: true, isComplete: true, isConsistent: true,
            hasSignature: true, isSignatureValid: true);
        var handler = CreateIntegrityHandler([pack]);
        var result = await handler.Handle(
            new GetEvidencePackIntegrityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Packs[0].Integrity.Should().Be(GetEvidencePackIntegrityReport.IntegrityStatus.Intact);
        result.Value.Packs[0].Tier.Should().Be(GetEvidencePackIntegrityReport.EvidencePackIntegrityTier.Trustworthy);
    }

    [Fact]
    public async Task BC2_ModifiedHash_Returns_IntegrityAnomaly()
    {
        var pack = MakePackEntry(isHashValid: false, itemCount: 3);
        var handler = CreateIntegrityHandler([pack]);
        var result = await handler.Handle(
            new GetEvidencePackIntegrityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IntegrityAnomalies.Should().HaveCount(1);
        result.Value.IntegrityAnomalies[0].Status.Should()
            .Be(GetEvidencePackIntegrityReport.IntegrityStatus.Modified);
    }

    [Fact]
    public async Task BC2_MissingItems_Returns_MissingIntegrity()
    {
        var pack = MakePackEntry(isHashValid: false, itemCount: 0);
        var handler = CreateIntegrityHandler([pack]);
        var result = await handler.Handle(
            new GetEvidencePackIntegrityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Packs[0].Integrity.Should().Be(GetEvidencePackIntegrityReport.IntegrityStatus.Missing);
    }

    [Fact]
    public async Task BC2_IncompleteCoherence_Returns_IncompleteStatus()
    {
        var pack = MakePackEntry(isHashValid: true, isComplete: false, isConsistent: true);
        var handler = CreateIntegrityHandler([pack]);
        var result = await handler.Handle(
            new GetEvidencePackIntegrityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Packs[0].Coherence.Should().Be(GetEvidencePackIntegrityReport.CoherenceStatus.Incomplete);
    }

    [Fact]
    public async Task BC2_InvalidSignature_ReducesScore()
    {
        var goodPack = MakePackEntry("pack-1", isHashValid: true, isComplete: true, isConsistent: true,
            hasSignature: true, isSignatureValid: true);
        var badSigPack = MakePackEntry("pack-2", isHashValid: true, isComplete: true, isConsistent: true,
            hasSignature: true, isSignatureValid: false);
        var handler = CreateIntegrityHandler([goodPack, badSigPack]);
        var result = await handler.Handle(
            new GetEvidencePackIntegrityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantEvidencePackScore.Should().BeLessThan(100m);
    }

    [Fact]
    public async Task BC2_ProductionReleasesWithInvalidEvidence_Counted()
    {
        var packs = new[]
        {
            MakePackEntry("pack-1", isProd: true, isHashValid: false, itemCount: 0), // invalid + prod
            MakePackEntry("pack-2", isProd: true, isHashValid: true, isComplete: true, isConsistent: true) // valid prod
        };
        var handler = CreateIntegrityHandler(packs);
        var result = await handler.Handle(
            new GetEvidencePackIntegrityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProductionReleasesWithInvalidEvidenceCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task BC2_IntactPacksCount_OnlyFullyIntactPacks()
    {
        var packs = new[]
        {
            MakePackEntry("pack-1", isHashValid: true),
            MakePackEntry("pack-2", isHashValid: false, itemCount: 5), // modified
        };
        var handler = CreateIntegrityHandler(packs);
        var result = await handler.Handle(
            new GetEvidencePackIntegrityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IntactPacks.Should().Be(1);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BC.3 — GetMultiDimensionalPromotionConfidenceReport
    // ═══════════════════════════════════════════════════════════════════════

    private static IMultiDimensionalPromotionConfidenceReader.PromotionDimensionData MakeDimensionData(
        string releaseId = "release-1",
        decimal blast = 90m, decimal rollback = 90m, decimal envBehavior = 90m,
        decimal evidenceIntegrity = 90m, decimal contractCompliance = 90m,
        decimal sloHealth = 90m, decimal chaosResilience = 90m, decimal changePattern = 90m)
        => new(
            ReleaseId: releaseId,
            ServiceId: "svc-1",
            BlastRadiusScore: blast,
            RollbackScore: rollback,
            EnvBehaviorScore: envBehavior,
            EvidenceIntegrityScore: evidenceIntegrity,
            ContractComplianceScore: contractCompliance,
            SloHealthScore: sloHealth,
            ChaosResilienceScore: chaosResilience,
            ChangePatternScore: changePattern,
            MissingDimensions: []);

    private static GetMultiDimensionalPromotionConfidenceReport.Handler CreateConfidenceHandler(
        IMultiDimensionalPromotionConfidenceReader.PromotionDimensionData data,
        IReadOnlyList<IMultiDimensionalPromotionConfidenceReader.HistoricalOutcomeEntry>? outcomes = null)
    {
        var reader = Substitute.For<IMultiDimensionalPromotionConfidenceReader>();
        reader.GetByReleaseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(data);
        reader.GetHistoricalOutcomesAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(outcomes ?? []);
        return new GetMultiDimensionalPromotionConfidenceReport.Handler(reader, CreateClock());
    }

    [Fact]
    public async Task BC3_AllDimensions90_Returns_HighConfidence_ProceedAuto()
    {
        var data = MakeDimensionData();
        var handler = CreateConfidenceHandler(data);
        var result = await handler.Handle(
            new GetMultiDimensionalPromotionConfidenceReport.Query(TenantId, "release-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetMultiDimensionalPromotionConfidenceReport.PromotionConfidenceTier.HighConfidence);
        result.Value.Recommendation.Should().Be(GetMultiDimensionalPromotionConfidenceReport.PromotionRecommendation.ProceedAutomatically);
    }

    [Fact]
    public async Task BC3_ScoreBelow40_Returns_BlockingIssues_Block()
    {
        var data = MakeDimensionData(blast: 20m, rollback: 20m, envBehavior: 20m, evidenceIntegrity: 20m,
            contractCompliance: 20m, sloHealth: 20m, chaosResilience: 20m, changePattern: 20m);
        var handler = CreateConfidenceHandler(data);
        var result = await handler.Handle(
            new GetMultiDimensionalPromotionConfidenceReport.Query(TenantId, "release-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetMultiDimensionalPromotionConfidenceReport.PromotionConfidenceTier.BlockingIssues);
        result.Value.Recommendation.Should().Be(GetMultiDimensionalPromotionConfidenceReport.PromotionRecommendation.Block);
    }

    [Fact]
    public async Task BC3_OneDimensionBelow30_IsBlocking()
    {
        // All high except one below blocking threshold (30)
        var data = MakeDimensionData(chaosResilience: 20m);
        var handler = CreateConfidenceHandler(data);
        var result = await handler.Handle(
            new GetMultiDimensionalPromotionConfidenceReport.Query(TenantId, "release-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BlockingFactors.Should().HaveCount(1);
        result.Value.BlockingFactors[0].DimensionName.Should().Be("ChaosResilience");
        result.Value.Tier.Should().Be(GetMultiDimensionalPromotionConfidenceReport.PromotionConfidenceTier.BlockingIssues);
    }

    [Fact]
    public async Task BC3_Score65_Returns_MediumConfidence_ProceedWithConditions()
    {
        // Average around 65: mix of high and medium scores, none below 30
        var data = MakeDimensionData(blast: 65m, rollback: 65m, envBehavior: 65m, evidenceIntegrity: 65m,
            contractCompliance: 65m, sloHealth: 65m, chaosResilience: 65m, changePattern: 65m);
        var handler = CreateConfidenceHandler(data);
        var result = await handler.Handle(
            new GetMultiDimensionalPromotionConfidenceReport.Query(TenantId, "release-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetMultiDimensionalPromotionConfidenceReport.PromotionConfidenceTier.MediumConfidence);
        result.Value.Recommendation.Should().Be(GetMultiDimensionalPromotionConfidenceReport.PromotionRecommendation.ProceedWithConditions);
    }

    [Fact]
    public async Task BC3_Score50_Returns_LowConfidence_RequireManualApproval()
    {
        var data = MakeDimensionData(blast: 50m, rollback: 50m, envBehavior: 50m, evidenceIntegrity: 50m,
            contractCompliance: 50m, sloHealth: 50m, chaosResilience: 50m, changePattern: 50m);
        var handler = CreateConfidenceHandler(data);
        var result = await handler.Handle(
            new GetMultiDimensionalPromotionConfidenceReport.Query(TenantId, "release-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be(GetMultiDimensionalPromotionConfidenceReport.PromotionConfidenceTier.LowConfidence);
        result.Value.Recommendation.Should().Be(GetMultiDimensionalPromotionConfidenceReport.PromotionRecommendation.RequireManualApproval);
    }

    [Fact]
    public async Task BC3_8Dimensions_AllPresent()
    {
        var data = MakeDimensionData();
        var handler = CreateConfidenceHandler(data);
        var result = await handler.Handle(
            new GetMultiDimensionalPromotionConfidenceReport.Query(TenantId, "release-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Dimensions.Should().HaveCount(8);
    }

    [Fact]
    public async Task BC3_HistoricalOutcomes_Populated()
    {
        var data = MakeDimensionData();
        var outcomes = new[]
        {
            new IMultiDimensionalPromotionConfidenceReader.HistoricalOutcomeEntry(
                "rel-1", 90m, true, FixedNow.AddDays(-30)),
            new IMultiDimensionalPromotionConfidenceReader.HistoricalOutcomeEntry(
                "rel-2", 85m, true, FixedNow.AddDays(-60)),
        };
        var handler = CreateConfidenceHandler(data, outcomes);
        var result = await handler.Handle(
            new GetMultiDimensionalPromotionConfidenceReport.Query(TenantId, "release-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.HistoricalOutcome.Should().NotBeNull();
        result.Value.HistoricalOutcome!.TotalPromotions.Should().Be(2);
    }

    [Fact]
    public async Task BC3_NoHistoricalOutcomes_HistoricalOutcomeIsNull()
    {
        var data = MakeDimensionData();
        var handler = CreateConfidenceHandler(data, []);
        var result = await handler.Handle(
            new GetMultiDimensionalPromotionConfidenceReport.Query(TenantId, "release-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.HistoricalOutcome.Should().BeNull();
    }

    [Fact]
    public async Task BC3_OverallScore_AverageOfAllDimensions()
    {
        // All dimensions = 80 → average = 80
        var data = MakeDimensionData(
            blast: 80m, rollback: 80m, envBehavior: 80m, evidenceIntegrity: 80m,
            contractCompliance: 80m, sloHealth: 80m, chaosResilience: 80m, changePattern: 80m);
        var handler = CreateConfidenceHandler(data);
        var result = await handler.Handle(
            new GetMultiDimensionalPromotionConfidenceReport.Query(TenantId, "release-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OverallConfidenceScore.Should().Be(80m);
    }
}
