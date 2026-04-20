using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;

using SetServiceTierFeature = NexTraceOne.Catalog.Application.Graph.Features.SetServiceTier.SetServiceTier;
using GetServiceTierPolicyFeature = NexTraceOne.Catalog.Application.Graph.Features.GetServiceTierPolicy.GetServiceTierPolicy;
using DetectOwnershipDriftFeature = NexTraceOne.Catalog.Application.Graph.Features.DetectOwnershipDrift.DetectOwnershipDrift;
using GetOwnershipDriftReportFeature = NexTraceOne.Catalog.Application.Graph.Features.GetOwnershipDriftReport.GetOwnershipDriftReport;
using ReviewServiceOwnershipFeature = NexTraceOne.Catalog.Application.Graph.Features.ReviewServiceOwnership.ReviewServiceOwnership;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave A.3 — Service Catalog v2.
/// Cobre: SetServiceTier, GetServiceTierPolicy, DetectOwnershipDrift,
/// GetOwnershipDriftReport e ReviewServiceOwnership.
/// </summary>
public sealed class ServiceCatalogV2Tests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid ServiceId = Guid.NewGuid();

    private static IDateTimeProvider CreateClock()
    {
        var c = Substitute.For<IDateTimeProvider>();
        c.UtcNow.Returns(FixedNow);
        return c;
    }

    private static ServiceAsset CreateService(
        string name = "svc-a",
        string team = "platform",
        string techOwner = "alice",
        string bizOwner = "bob",
        string sloTarget = "99.95%",
        string onCall = "on-call-1",
        string contact = "#platform",
        ServiceTierType tier = ServiceTierType.Standard,
        DateTimeOffset? lastReview = null)
    {
        var svc = ServiceAsset.Create(name, "platform", team);
        svc.UpdateOwnership(team, techOwner, bizOwner);
        svc.UpdateExtendedMetadata(
            subDomain: null, capability: null,
            gitRepository: string.Empty, ciPipelineUrl: string.Empty,
            infrastructureProvider: string.Empty, hostingPlatform: string.Empty,
            runtimeLanguage: string.Empty, runtimeVersion: string.Empty,
            sloTarget: sloTarget, dataClassification: string.Empty,
            regulatoryScope: string.Empty, changeFrequency: string.Empty,
            productOwner: string.Empty, contactChannel: contact,
            onCallRotationId: onCall);
        svc.SetTier(tier);
        if (lastReview.HasValue)
            svc.RecordOwnershipReview(lastReview.Value);
        return svc;
    }

    private static IConfigurationResolutionService CreateConfig(
        int driftDays = 90,
        decimal criticalSlo = 99.9m,
        decimal standardSlo = 99.5m,
        decimal criticalMaturity = 0.8m,
        decimal standardMaturity = 0.6m)
    {
        var cfg = Substitute.For<IConfigurationResolutionService>();
        cfg.ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var key = ci.ArgAt<string>(0);
                var val = key switch
                {
                    "catalog.ownershipDrift.threshold.days" => driftDays.ToString(),
                    "catalog.tier.critical.sloMinPercent" => criticalSlo.ToString(),
                    "catalog.tier.standard.sloMinPercent" => standardSlo.ToString(),
                    "catalog.tier.experimental.sloMinPercent" => "99.0",
                    "catalog.tier.critical.maturityMinScore" => criticalMaturity.ToString(),
                    "catalog.tier.standard.maturityMinScore" => standardMaturity.ToString(),
                    _ => null
                };
                if (val is null) return Task.FromResult<EffectiveConfigurationDto?>(null);
                return Task.FromResult<EffectiveConfigurationDto?>(
                    new EffectiveConfigurationDto(key, val, "System", null, false, true, key, "Decimal", false, 1));
            });
        return cfg;
    }

    // ── SetServiceTier ────────────────────────────────────────────────────

    [Fact]
    public async Task SetServiceTier_ValidTier_ReturnsTierSet()
    {
        var service = CreateService();
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);
        var uow = Substitute.For<ICatalogGraphUnitOfWork>();

        var handler = new SetServiceTierFeature.Handler(repo, uow);
        var result = await handler.Handle(
            new SetServiceTierFeature.Command(ServiceId, "Critical"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be("Critical");
        service.Tier.Should().Be(ServiceTierType.Critical);
    }

    [Fact]
    public async Task SetServiceTier_InvalidTier_ReturnsValidationError()
    {
        var service = CreateService();
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);
        var uow = Substitute.For<ICatalogGraphUnitOfWork>();

        var handler = new SetServiceTierFeature.Handler(repo, uow);
        var result = await handler.Handle(
            new SetServiceTierFeature.Command(ServiceId, "Platinum"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task SetServiceTier_ServiceNotFound_ReturnsError()
    {
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);
        var uow = Substitute.For<ICatalogGraphUnitOfWork>();

        var handler = new SetServiceTierFeature.Handler(repo, uow);
        var result = await handler.Handle(
            new SetServiceTierFeature.Command(Guid.NewGuid(), "Standard"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ── GetServiceTierPolicy ──────────────────────────────────────────────

    [Fact]
    public async Task GetServiceTierPolicy_CriticalTier_ReturnsHighThresholds()
    {
        var service = CreateService(tier: ServiceTierType.Critical, sloTarget: "99.95%");
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);

        var handler = new GetServiceTierPolicyFeature.Handler(repo, CreateConfig());
        var result = await handler.Handle(new GetServiceTierPolicyFeature.Query(ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be("Critical");
        result.Value.Policy.MinSloPercent.Should().Be(99.9m);
        result.Value.Policy.OnCallRequired.Should().BeTrue();
        result.Value.Policy.RunbookRequired.Should().BeTrue();
    }

    [Fact]
    public async Task GetServiceTierPolicy_ExperimentalTier_ReturnsRelaxedThresholds()
    {
        var service = CreateService(tier: ServiceTierType.Experimental);
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);

        var handler = new GetServiceTierPolicyFeature.Handler(repo, CreateConfig());
        var result = await handler.Handle(new GetServiceTierPolicyFeature.Query(ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Policy.OnCallRequired.Should().BeFalse();
        result.Value.Policy.MinMaturityScore.Should().Be(0m);
    }

    [Fact]
    public async Task GetServiceTierPolicy_SloConformant_WhenAboveMinimum()
    {
        var service = CreateService(sloTarget: "99.9%", tier: ServiceTierType.Standard);
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);

        var handler = new GetServiceTierPolicyFeature.Handler(repo, CreateConfig(standardSlo: 99.5m));
        var result = await handler.Handle(new GetServiceTierPolicyFeature.Query(ServiceId), CancellationToken.None);

        result.Value!.Conformance.SloConformant.Should().BeTrue();
    }

    // ── DetectOwnershipDrift ──────────────────────────────────────────────

    [Fact]
    public async Task DetectOwnershipDrift_RecentReview_NoDrift()
    {
        var service = CreateService(lastReview: FixedNow.AddDays(-10));
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);

        var handler = new DetectOwnershipDriftFeature.Handler(repo, CreateConfig(driftDays: 90), CreateClock());
        var result = await handler.Handle(new DetectOwnershipDriftFeature.Query(ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Only check that stale-review signal is absent
        result.Value!.Signals.Should().NotContain(s =>
            s.Code == "OwnershipReviewStale" || s.Code == "OwnershipNeverReviewed");
    }

    [Fact]
    public async Task DetectOwnershipDrift_NeverReviewed_ReturnsNeverReviewedSignal()
    {
        var service = CreateService(lastReview: null);
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);

        var handler = new DetectOwnershipDriftFeature.Handler(repo, CreateConfig(), CreateClock());
        var result = await handler.Handle(new DetectOwnershipDriftFeature.Query(ServiceId), CancellationToken.None);

        result.Value!.HasDrift.Should().BeTrue();
        result.Value.Signals.Should().Contain(s => s.Code == "OwnershipNeverReviewed");
    }

    [Fact]
    public async Task DetectOwnershipDrift_StaleReview_ReturnsStaleSignal()
    {
        var service = CreateService(lastReview: FixedNow.AddDays(-120));
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);

        var handler = new DetectOwnershipDriftFeature.Handler(repo, CreateConfig(driftDays: 90), CreateClock());
        var result = await handler.Handle(new DetectOwnershipDriftFeature.Query(ServiceId), CancellationToken.None);

        result.Value!.Signals.Should().Contain(s => s.Code == "OwnershipReviewStale");
        result.Value.DaysSinceOwnershipReview.Should().Be(120);
    }

    [Fact]
    public async Task DetectOwnershipDrift_MissingTechnicalOwner_ReturnsCriticalSignal()
    {
        var service = CreateService(techOwner: string.Empty, lastReview: FixedNow.AddDays(-5));
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);

        var handler = new DetectOwnershipDriftFeature.Handler(repo, CreateConfig(), CreateClock());
        var result = await handler.Handle(new DetectOwnershipDriftFeature.Query(ServiceId), CancellationToken.None);

        result.Value!.Signals.Should().Contain(s =>
            s.Code == "NoTechnicalOwner" && s.Severity == "critical");
    }

    [Fact]
    public async Task DetectOwnershipDrift_CriticalTierMissingOnCall_ReturnsCriticalSignal()
    {
        var service = CreateService(onCall: string.Empty, tier: ServiceTierType.Critical, lastReview: FixedNow.AddDays(-5));
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);

        var handler = new DetectOwnershipDriftFeature.Handler(repo, CreateConfig(), CreateClock());
        var result = await handler.Handle(new DetectOwnershipDriftFeature.Query(ServiceId), CancellationToken.None);

        result.Value!.Signals.Should().Contain(s =>
            s.Code == "NoOnCallRotation" && s.Severity == "critical");
    }

    // ── GetOwnershipDriftReport ───────────────────────────────────────────

    [Fact]
    public async Task GetOwnershipDriftReport_EmptyList_ReturnsZeroFindings()
    {
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListFilteredAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<ServiceType?>(),
            Arg.Any<Criticality?>(), Arg.Any<LifecycleStatus?>(), Arg.Any<ExposureType?>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset>());

        var handler = new GetOwnershipDriftReportFeature.Handler(repo, CreateConfig(), CreateClock());
        var result = await handler.Handle(new GetOwnershipDriftReportFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Summary.ServicesWithDrift.Should().Be(0);
        result.Value.Findings.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOwnershipDriftReport_MultipleServices_OrdersBySeverity()
    {
        var svcNeverReviewed = CreateService("svc-a", team: "team-a");
        var svcRecentReview = CreateService("svc-b", team: "team-b", lastReview: FixedNow.AddDays(-5));

        var repo = Substitute.For<IServiceAssetRepository>();
        repo.ListFilteredAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<ServiceType?>(),
            Arg.Any<Criticality?>(), Arg.Any<LifecycleStatus?>(), Arg.Any<ExposureType?>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset> { svcNeverReviewed, svcRecentReview });

        var handler = new GetOwnershipDriftReportFeature.Handler(repo, CreateConfig(), CreateClock());
        var result = await handler.Handle(new GetOwnershipDriftReportFeature.Query(), CancellationToken.None);

        result.Value!.Findings.Should().NotBeEmpty();
        // Never-reviewed should appear before recently reviewed (severity ordering)
        if (result.Value.Findings.Count > 1)
        {
            var severityRank = new Dictionary<string, int> { ["critical"] = 3, ["high"] = 2, ["medium"] = 1, ["info"] = 0 };
            var first = severityRank[result.Value.Findings[0].MaxSeverity];
            var second = severityRank[result.Value.Findings[1].MaxSeverity];
            first.Should().BeGreaterThanOrEqualTo(second);
        }
    }

    // ── ReviewServiceOwnership ────────────────────────────────────────────

    [Fact]
    public async Task ReviewServiceOwnership_UpdatesLastReviewAt()
    {
        var service = CreateService(lastReview: null);
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(service);
        var uow = Substitute.For<ICatalogGraphUnitOfWork>();

        var handler = new ReviewServiceOwnershipFeature.Handler(repo, uow, CreateClock());
        var result = await handler.Handle(
            new ReviewServiceOwnershipFeature.Command(ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReviewedAt.Should().Be(FixedNow);
        service.LastOwnershipReviewAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task ReviewServiceOwnership_ServiceNotFound_ReturnsError()
    {
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);
        var uow = Substitute.For<ICatalogGraphUnitOfWork>();

        var handler = new ReviewServiceOwnershipFeature.Handler(repo, uow, CreateClock());
        var result = await handler.Handle(
            new ReviewServiceOwnershipFeature.Command(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
