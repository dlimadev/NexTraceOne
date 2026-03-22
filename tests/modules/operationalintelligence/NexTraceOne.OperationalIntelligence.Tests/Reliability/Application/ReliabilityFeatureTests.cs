using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetDomainReliabilitySummary;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityCoverage;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityDetail;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityTrend;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetTeamReliabilitySummary;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetTeamReliabilityTrend;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.ListServiceReliability;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;
using Xunit;

namespace NexTraceOne.OperationalIntelligence.Tests.Reliability.Application;

/// <summary>
/// Testes unitários para as features do subdomínio Reliability.
/// Verificam handlers com superfícies de dados mockadas via NSubstitute.
/// </summary>
public sealed class ReliabilityFeatureTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static ICurrentTenant MockTenant()
    {
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(TenantId);
        return tenant;
    }

    // ── ListServiceReliability ───────────────────────────────────────

    [Fact]
    public async Task ListServiceReliability_WithRealData_ShouldReturnItems()
    {
        var runtimeSurface = Substitute.For<IReliabilityRuntimeSurface>();
        var incidentSurface = Substitute.For<IReliabilityIncidentSurface>();

        runtimeSurface.GetLatestSignalsAllServicesAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<RuntimeServiceSignal>
            {
                new("svc-order-api", "production", "Healthy", 0.01m, 80m, 100m, DateTimeOffset.UtcNow)
            });
        runtimeSurface.GetObservabilityScoresAllServicesAsync(null, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, decimal> { ["svc-order-api"] = 0.9m });
        incidentSurface.GetAllServicesIncidentSignalsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilityIncidentSignal>());

        var handler = new ListServiceReliability.Handler(runtimeSurface, incidentSurface, MockTenant());
        var query = new ListServiceReliability.Query(null, null, null, null, null, null, null, null, 1, 100);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().NotBeEmpty();
        result.Value.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ListServiceReliability_FilterByStatus_ShouldReturnFiltered()
    {
        var runtimeSurface = Substitute.For<IReliabilityRuntimeSurface>();
        var incidentSurface = Substitute.For<IReliabilityIncidentSurface>();

        runtimeSurface.GetLatestSignalsAllServicesAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<RuntimeServiceSignal>
            {
                new("svc-degraded", "production", "Degraded", 0.1m, 500m, 50m, DateTimeOffset.UtcNow),
                new("svc-healthy", "production", "Healthy", 0.0m, 50m, 200m, DateTimeOffset.UtcNow)
            });
        runtimeSurface.GetObservabilityScoresAllServicesAsync(null, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, decimal>());
        incidentSurface.GetAllServicesIncidentSignalsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilityIncidentSignal>());

        var handler = new ListServiceReliability.Handler(runtimeSurface, incidentSurface, MockTenant());
        var query = new ListServiceReliability.Query(null, null, null, null, ReliabilityStatus.Degraded, null, null, null, 1, 100);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().AllSatisfy(i => i.ReliabilityStatus.Should().Be(ReliabilityStatus.Degraded));
    }

    [Fact]
    public async Task ListServiceReliability_FilterBySearch_ShouldReturnMatching()
    {
        var runtimeSurface = Substitute.For<IReliabilityRuntimeSurface>();
        var incidentSurface = Substitute.For<IReliabilityIncidentSurface>();

        runtimeSurface.GetLatestSignalsAllServicesAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<RuntimeServiceSignal>
            {
                new("svc-payment-gateway", "production", "Healthy", 0.01m, 100m, 50m, DateTimeOffset.UtcNow),
                new("svc-order-api", "production", "Healthy", 0.01m, 80m, 100m, DateTimeOffset.UtcNow)
            });
        runtimeSurface.GetObservabilityScoresAllServicesAsync(null, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, decimal>());
        incidentSurface.GetAllServicesIncidentSignalsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilityIncidentSignal>());

        var handler = new ListServiceReliability.Handler(runtimeSurface, incidentSurface, MockTenant());
        var query = new ListServiceReliability.Query(null, null, null, null, null, null, null, "payment", 1, 100);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
    }

    [Fact]
    public void ListServiceReliability_Validator_ShouldRejectInvalidPage()
    {
        var validator = new ListServiceReliability.Validator();
        var query = new ListServiceReliability.Query(null, null, null, null, null, null, null, null, 0, 10);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListServiceReliability_Validator_ShouldAcceptValidQuery()
    {
        var validator = new ListServiceReliability.Validator();
        var query = new ListServiceReliability.Query("team-a", null, null, null, null, null, null, null, 1, 20);

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    // ── GetServiceReliabilityDetail ──────────────────────────────────

    [Fact]
    public async Task GetServiceReliabilityDetail_KnownService_ShouldReturnDetail()
    {
        var runtimeSurface = Substitute.For<IReliabilityRuntimeSurface>();
        var incidentSurface = Substitute.For<IReliabilityIncidentSurface>();
        var snapshotRepo = Substitute.For<IReliabilitySnapshotRepository>();

        runtimeSurface.GetLatestSignalAsync("svc-order-api", string.Empty, Arg.Any<CancellationToken>())
            .Returns(new RuntimeServiceSignal("svc-order-api", "production", "Healthy", 0.01m, 80m, 100m, DateTimeOffset.UtcNow));
        runtimeSurface.GetObservabilityScoreAsync("svc-order-api", "production", Arg.Any<CancellationToken>())
            .Returns((decimal?)0.9m);
        incidentSurface.GetActiveIncidentsAsync("svc-order-api", TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilityIncidentSignal>());
        snapshotRepo.GetHistoryAsync("svc-order-api", TenantId, 2, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilitySnapshot>());

        var handler = new GetServiceReliabilityDetail.Handler(runtimeSurface, incidentSurface, snapshotRepo, MockTenant());
        var query = new GetServiceReliabilityDetail.Query("svc-order-api");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Identity.ServiceId.Should().Be("svc-order-api");
        result.Value.Status.Should().Be(ReliabilityStatus.Healthy);
        result.Value.Coverage.HasOperationalSignals.Should().BeTrue();
    }

    [Fact]
    public async Task GetServiceReliabilityDetail_DegradedService_ShouldShowFlags()
    {
        var runtimeSurface = Substitute.For<IReliabilityRuntimeSurface>();
        var incidentSurface = Substitute.For<IReliabilityIncidentSurface>();
        var snapshotRepo = Substitute.For<IReliabilitySnapshotRepository>();

        runtimeSurface.GetLatestSignalAsync("svc-payment-gateway", string.Empty, Arg.Any<CancellationToken>())
            .Returns(new RuntimeServiceSignal("svc-payment-gateway", "production", "Degraded", 0.15m, 800m, 50m, DateTimeOffset.UtcNow));
        runtimeSurface.GetObservabilityScoreAsync("svc-payment-gateway", "production", Arg.Any<CancellationToken>())
            .Returns((decimal?)0.6m);
        incidentSurface.GetActiveIncidentsAsync("svc-payment-gateway", TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilityIncidentSignal>());
        snapshotRepo.GetHistoryAsync("svc-payment-gateway", TenantId, 2, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilitySnapshot>());

        var handler = new GetServiceReliabilityDetail.Handler(runtimeSurface, incidentSurface, snapshotRepo, MockTenant());
        var query = new GetServiceReliabilityDetail.Query("svc-payment-gateway");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ReliabilityStatus.Degraded);
        result.Value.ActiveFlags.HasFlag(OperationalFlag.AnomalyDetected).Should().BeTrue();
    }

    [Fact]
    public async Task GetServiceReliabilityDetail_UnknownService_ShouldReturnNotFound()
    {
        var runtimeSurface = Substitute.For<IReliabilityRuntimeSurface>();
        var incidentSurface = Substitute.For<IReliabilityIncidentSurface>();
        var snapshotRepo = Substitute.For<IReliabilitySnapshotRepository>();

        runtimeSurface.GetLatestSignalAsync("svc-nonexistent", string.Empty, Arg.Any<CancellationToken>())
            .Returns((RuntimeServiceSignal?)null);
        incidentSurface.GetActiveIncidentsAsync("svc-nonexistent", TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilityIncidentSignal>());
        snapshotRepo.GetHistoryAsync("svc-nonexistent", TenantId, 2, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilitySnapshot>());

        var handler = new GetServiceReliabilityDetail.Handler(runtimeSurface, incidentSurface, snapshotRepo, MockTenant());
        var query = new GetServiceReliabilityDetail.Query("svc-nonexistent");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void GetServiceReliabilityDetail_Validator_ShouldRejectEmptyId()
    {
        var validator = new GetServiceReliabilityDetail.Validator();
        var query = new GetServiceReliabilityDetail.Query("");

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    // ── GetTeamReliabilitySummary ────────────────────────────────────

    [Fact]
    public async Task GetTeamReliabilitySummary_KnownTeam_ShouldReturnSummary()
    {
        var incidentSurface = Substitute.For<IReliabilityIncidentSurface>();
        incidentSurface.GetTeamIncidentsAsync("order-squad", TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilityIncidentSignal>
            {
                new("svc-order-api", "Order API", "order-squad", "Minor", "Open", DateTimeOffset.UtcNow)
            });

        var handler = new GetTeamReliabilitySummary.Handler(incidentSurface, MockTenant());
        var query = new GetTeamReliabilitySummary.Query("order-squad");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TeamId.Should().Be("order-squad");
        result.Value.TotalServices.Should().BeGreaterThan(0);
        result.Value.HealthyServices.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetTeamReliabilitySummary_UnknownTeam_ShouldReturnZeros()
    {
        var incidentSurface = Substitute.For<IReliabilityIncidentSurface>();
        incidentSurface.GetTeamIncidentsAsync("unknown-squad", TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilityIncidentSignal>());

        var handler = new GetTeamReliabilitySummary.Handler(incidentSurface, MockTenant());
        var query = new GetTeamReliabilitySummary.Query("unknown-squad");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServices.Should().Be(0);
    }

    // ── GetDomainReliabilitySummary ──────────────────────────────────

    [Fact]
    public async Task GetDomainReliabilitySummary_KnownDomain_ShouldReturnSummary()
    {
        var incidentSurface = Substitute.For<IReliabilityIncidentSurface>();
        incidentSurface.GetDomainIncidentsAsync("Orders", TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilityIncidentSignal>
            {
                new("svc-order-api", "Order API", "order-squad", "Minor", "Open", DateTimeOffset.UtcNow)
            });

        var handler = new GetDomainReliabilitySummary.Handler(incidentSurface, MockTenant());
        var query = new GetDomainReliabilitySummary.Query("Orders");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DomainId.Should().Be("Orders");
        result.Value.TotalServices.Should().BeGreaterThan(0);
    }

    // ── GetServiceReliabilityTrend ──────────────────────────────────

    [Fact]
    public async Task GetServiceReliabilityTrend_NoHistory_ShouldReturnStable()
    {
        var snapshotRepo = Substitute.For<IReliabilitySnapshotRepository>();
        snapshotRepo.GetHistoryAsync("svc-order-api", TenantId, 30, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilitySnapshot>());

        var handler = new GetServiceReliabilityTrend.Handler(snapshotRepo, MockTenant());
        var query = new GetServiceReliabilityTrend.Query("svc-order-api");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Direction.Should().Be(TrendDirection.Stable);
    }

    [Fact]
    public async Task GetServiceReliabilityTrend_Validator_ShouldRejectEmptyId()
    {
        var validator = new GetServiceReliabilityTrend.Validator();
        var query = new GetServiceReliabilityTrend.Query("");

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    // ── GetTeamReliabilityTrend ─────────────────────────────────────

    [Fact]
    public async Task GetTeamReliabilityTrend_ShouldReturnTrend()
    {
        var incidentSurface = Substitute.For<IReliabilityIncidentSurface>();
        incidentSurface.GetTeamIncidentsAsync("order-squad", TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilityIncidentSignal>
            {
                new("svc-order-api", "Order API", "order-squad", "Minor", "Open", DateTimeOffset.UtcNow)
            });

        var handler = new GetTeamReliabilityTrend.Handler(incidentSurface, MockTenant());
        var query = new GetTeamReliabilityTrend.Query("order-squad");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TeamId.Should().Be("order-squad");
        result.Value.DataPoints.Should().NotBeEmpty();
    }

    // ── GetServiceReliabilityCoverage ────────────────────────────────

    [Fact]
    public async Task GetServiceReliabilityCoverage_WellCoveredService_ShouldShowOperationalSignals()
    {
        var runtimeSurface = Substitute.For<IReliabilityRuntimeSurface>();
        var incidentSurface = Substitute.For<IReliabilityIncidentSurface>();
        var snapshotRepo = Substitute.For<IReliabilitySnapshotRepository>();

        runtimeSurface.GetLatestSignalAsync("svc-order-api", string.Empty, Arg.Any<CancellationToken>())
            .Returns(new RuntimeServiceSignal("svc-order-api", "production", "Healthy", 0.01m, 80m, 100m, DateTimeOffset.UtcNow));
        incidentSurface.HasRunbookAsync("svc-order-api", Arg.Any<CancellationToken>()).Returns(true);
        incidentSurface.GetActiveIncidentsAsync("svc-order-api", TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilityIncidentSignal>
            {
                new("svc-order-api", "Order API", "order-squad", "Minor", "Open", DateTimeOffset.UtcNow)
            });

        var handler = new GetServiceReliabilityCoverage.Handler(runtimeSurface, incidentSurface, snapshotRepo, MockTenant());
        var query = new GetServiceReliabilityCoverage.Query("svc-order-api");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasOperationalSignals.Should().BeTrue();
        result.Value.HasRunbook.Should().BeTrue();
        result.Value.HasOwner.Should().BeTrue();
    }

    [Fact]
    public async Task GetServiceReliabilityCoverage_NoData_ShouldShowGaps()
    {
        var runtimeSurface = Substitute.For<IReliabilityRuntimeSurface>();
        var incidentSurface = Substitute.For<IReliabilityIncidentSurface>();
        var snapshotRepo = Substitute.For<IReliabilitySnapshotRepository>();

        runtimeSurface.GetLatestSignalAsync("svc-report-scheduler", string.Empty, Arg.Any<CancellationToken>())
            .Returns((RuntimeServiceSignal?)null);
        incidentSurface.HasRunbookAsync("svc-report-scheduler", Arg.Any<CancellationToken>()).Returns(false);
        incidentSurface.GetActiveIncidentsAsync("svc-report-scheduler", TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ReliabilityIncidentSignal>());

        var handler = new GetServiceReliabilityCoverage.Handler(runtimeSurface, incidentSurface, snapshotRepo, MockTenant());
        var query = new GetServiceReliabilityCoverage.Query("svc-report-scheduler");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasOperationalSignals.Should().BeFalse();
        result.Value.HasRunbook.Should().BeFalse();
        result.Value.HasOwner.Should().BeFalse();
    }

    [Fact]
    public void GetServiceReliabilityCoverage_Validator_ShouldRejectEmptyId()
    {
        var validator = new GetServiceReliabilityCoverage.Validator();
        var query = new GetServiceReliabilityCoverage.Query("");

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }
}
