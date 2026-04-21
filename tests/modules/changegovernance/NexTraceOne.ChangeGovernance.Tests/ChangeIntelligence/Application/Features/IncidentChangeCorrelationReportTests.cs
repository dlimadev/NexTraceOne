using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetIncidentChangeCorrelationReport;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using IncidentCorrelationRisk = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetIncidentChangeCorrelationReport.GetIncidentChangeCorrelationReport.IncidentCorrelationRisk;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para Wave R.1 — GetIncidentChangeCorrelationReport.
/// Cobre: relatório vazio, releases sem incidentes, classificação de risco (Low/Medium/High/Critical),
/// múltiplos serviços, taxa global de incidente, top serviços por taxa e por contagem absoluta.
/// </summary>
public sealed class IncidentChangeCorrelationReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static Release MakeRelease(string serviceName, string version = "1.0.0", string env = "prod")
        => Release.Create(TenantId, Guid.NewGuid(), serviceName, version, env,
            "pipeline-ci", "abc123", FixedNow.AddDays(-1));

    private static ChangeEvent MakeIncidentEvent(ReleaseId releaseId)
        => ChangeEvent.Create(releaseId, "incident_correlated", "incident detected", "monitor", FixedNow);

    private static GetIncidentChangeCorrelationReport.Handler CreateHandler(
        IReadOnlyList<Release> releases,
        IReadOnlyDictionary<ReleaseId, IReadOnlyList<ChangeEvent>> incidentMap)
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var changeEventRepo = Substitute.For<IChangeEventRepository>();

        releaseRepo.ListInRangeAsync(
                Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<string?>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(releases);

        foreach (var release in releases)
        {
            var events = incidentMap.TryGetValue(release.Id, out var evts)
                ? evts
                : (IReadOnlyList<ChangeEvent>)[];

            changeEventRepo.ListByReleaseIdAndEventTypeAsync(
                    Arg.Is<ReleaseId>(id => id == release.Id),
                    "incident_correlated",
                    Arg.Any<CancellationToken>())
                .Returns(events);
        }

        return new GetIncidentChangeCorrelationReport.Handler(releaseRepo, changeEventRepo, CreateClock());
    }

    private static GetIncidentChangeCorrelationReport.Query DefaultQuery()
        => new(TenantId: TenantId.ToString(), LookbackDays: 90, TopServicesCount: 10);

    // ── Empty: no releases ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoReleases_ReturnsZeroTotals()
    {
        var handler = CreateHandler([], new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>());
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(0, r.TotalReleasesInPeriod);
        Assert.Equal(0, r.ReleasesWithCorrelatedIncident);
        Assert.Equal(0m, r.TenantIncidentRatePct);
        Assert.Empty(r.TopServicesByIncidentRate);
        Assert.Empty(r.TopServicesByAbsoluteIncidentCount);
    }

    // ── Releases with no incident events ─────────────────────────────────

    [Fact]
    public async Task Handle_ReleasesWithNoIncidents_ZeroIncidentCount()
    {
        var r1 = MakeRelease("svc-a");
        var r2 = MakeRelease("svc-b");

        var handler = CreateHandler([r1, r2], new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>());
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(2, r.TotalReleasesInPeriod);
        Assert.Equal(0, r.ReleasesWithCorrelatedIncident);
        Assert.Equal(0m, r.TenantIncidentRatePct);
        Assert.Equal(0, r.DistinctServicesWithIncident);
    }

    // ── All releases have incidents ───────────────────────────────────────

    [Fact]
    public async Task Handle_AllReleasesWithIncidents_Rate100Pct()
    {
        var r1 = MakeRelease("svc-a");
        var incidentMap = new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>
        {
            [r1.Id] = [MakeIncidentEvent(r1.Id)]
        };

        var handler = CreateHandler([r1], incidentMap);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(1, r.TotalReleasesInPeriod);
        Assert.Equal(1, r.ReleasesWithCorrelatedIncident);
        Assert.Equal(100m, r.TenantIncidentRatePct);
        Assert.Equal(1, r.DistinctServicesWithIncident);
    }

    // ── Risk classification: Low ──────────────────────────────────────────

    [Fact]
    public async Task Handle_LowIncidentRate_ClassifiesAsLow()
    {
        // 1 incident out of 25 releases = 4% → Low
        var releases = Enumerable.Range(1, 25).Select(i => MakeRelease("svc-x", $"1.{i}.0")).ToList();
        var incidentMap = new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>
        {
            [releases[0].Id] = [MakeIncidentEvent(releases[0].Id)]
        };

        var handler = CreateHandler(releases, incidentMap);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var r = result.Value;
        var entry = r.TopServicesByIncidentRate.First();
        Assert.Equal(IncidentCorrelationRisk.Low, entry.RiskTier);
        Assert.Equal(1, r.RiskDistribution.LowCount);
        Assert.Equal(0, r.RiskDistribution.MediumCount);
    }

    // ── Risk classification: Medium ───────────────────────────────────────

    [Fact]
    public async Task Handle_MediumIncidentRate_ClassifiesAsMedium()
    {
        // 1 out of 10 = 10% → Medium
        var releases = Enumerable.Range(1, 10).Select(i => MakeRelease("svc-y", $"1.{i}.0")).ToList();
        var incidentMap = new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>
        {
            [releases[0].Id] = [MakeIncidentEvent(releases[0].Id)]
        };

        var handler = CreateHandler(releases, incidentMap);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var entry = result.Value.TopServicesByIncidentRate.First();
        Assert.Equal(IncidentCorrelationRisk.Medium, entry.RiskTier);
        Assert.Equal(1, result.Value.RiskDistribution.MediumCount);
    }

    // ── Risk classification: High ─────────────────────────────────────────

    [Fact]
    public async Task Handle_HighIncidentRate_ClassifiesAsHigh()
    {
        // 2 out of 10 = 20% → High
        var releases = Enumerable.Range(1, 10).Select(i => MakeRelease("svc-z", $"1.{i}.0")).ToList();
        var incidentMap = new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>
        {
            [releases[0].Id] = [MakeIncidentEvent(releases[0].Id)],
            [releases[1].Id] = [MakeIncidentEvent(releases[1].Id)]
        };

        var handler = CreateHandler(releases, incidentMap);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var entry = result.Value.TopServicesByIncidentRate.First();
        Assert.Equal(IncidentCorrelationRisk.High, entry.RiskTier);
        Assert.Equal(1, result.Value.RiskDistribution.HighCount);
    }

    // ── Risk classification: Critical ─────────────────────────────────────

    [Fact]
    public async Task Handle_CriticalIncidentRate_ClassifiesAsCritical()
    {
        // 4 out of 10 = 40% → Critical
        var releases = Enumerable.Range(1, 10).Select(i => MakeRelease("svc-w", $"1.{i}.0")).ToList();
        var incidentMap = releases.Take(4).ToDictionary(
            r => r.Id,
            r => (IReadOnlyList<ChangeEvent>)[MakeIncidentEvent(r.Id)]);

        var handler = CreateHandler(releases, incidentMap);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var entry = result.Value.TopServicesByIncidentRate.First();
        Assert.Equal(IncidentCorrelationRisk.Critical, entry.RiskTier);
        Assert.Equal(1, result.Value.RiskDistribution.CriticalCount);
    }

    // ── Multiple services grouped correctly ───────────────────────────────

    [Fact]
    public async Task Handle_MultipleServices_GroupsByServiceName()
    {
        var svcA_1 = MakeRelease("svc-a", "1.0.0");
        var svcA_2 = MakeRelease("svc-a", "1.1.0");
        var svcB_1 = MakeRelease("svc-b", "2.0.0");

        var incidentMap = new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>
        {
            [svcA_1.Id] = [MakeIncidentEvent(svcA_1.Id)]
        };

        var handler = CreateHandler([svcA_1, svcA_2, svcB_1], incidentMap);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var r = result.Value;

        Assert.Equal(3, r.TotalReleasesInPeriod);
        Assert.Equal(1, r.ReleasesWithCorrelatedIncident);
        Assert.Equal(2, r.TopServicesByIncidentRate.Count);

        var svcAEntry = r.TopServicesByIncidentRate.First(e => e.ServiceName == "svc-a");
        Assert.Equal(2, svcAEntry.TotalReleases);
        Assert.Equal(1, svcAEntry.ReleasesWithIncident);
        Assert.Equal(50m, svcAEntry.IncidentRatePct);

        var svcBEntry = r.TopServicesByIncidentRate.First(e => e.ServiceName == "svc-b");
        Assert.Equal(0, svcBEntry.ReleasesWithIncident);
    }

    // ── Tenant incident rate calculation ──────────────────────────────────

    [Fact]
    public async Task Handle_TenantIncidentRate_IsGlobal()
    {
        var r1 = MakeRelease("svc-a");
        var r2 = MakeRelease("svc-b");
        var r3 = MakeRelease("svc-c");

        // 1 out of 3 = 33.33%
        var incidentMap = new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>
        {
            [r1.Id] = [MakeIncidentEvent(r1.Id)]
        };

        var handler = CreateHandler([r1, r2, r3], incidentMap);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(33.33m, result.Value.TenantIncidentRatePct);
    }

    // ── Top services by rate ordered correctly ────────────────────────────

    [Fact]
    public async Task Handle_TopByRate_OrderedByIncidentRateDescending()
    {
        var releases = new List<Release>
        {
            MakeRelease("svc-a", "1.0"), // will have 50% rate (1/2)
            MakeRelease("svc-a", "1.1"),
            MakeRelease("svc-b", "1.0"), // will have 100% rate (1/1)
            MakeRelease("svc-c", "1.0"), // will have 0%
        };

        var incidentMap = new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>
        {
            [releases[0].Id] = [MakeIncidentEvent(releases[0].Id)],
            [releases[2].Id] = [MakeIncidentEvent(releases[2].Id)]
        };

        var handler = CreateHandler(releases, incidentMap);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var top = result.Value.TopServicesByIncidentRate;
        Assert.True(top.Count >= 2);
        Assert.True(top[0].IncidentRatePct >= top[1].IncidentRatePct);
    }

    // ── Top services by absolute count ordered correctly ──────────────────

    [Fact]
    public async Task Handle_TopByAbsoluteCount_OrderedByIncidentCountDescending()
    {
        // svc-a: 5 releases, 3 incidents; svc-b: 2 releases, 2 incidents
        var svcA = Enumerable.Range(1, 5).Select(i => MakeRelease("svc-a", $"1.{i}.0")).ToList();
        var svcB = Enumerable.Range(1, 2).Select(i => MakeRelease("svc-b", $"2.{i}.0")).ToList();

        var incidentMap = new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>();
        foreach (var r in svcA.Take(3))
            incidentMap[r.Id] = [MakeIncidentEvent(r.Id)];
        foreach (var r in svcB.Take(2))
            incidentMap[r.Id] = [MakeIncidentEvent(r.Id)];

        var handler = CreateHandler([.. svcA, .. svcB], incidentMap);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var top = result.Value.TopServicesByAbsoluteIncidentCount;
        Assert.True(top[0].ReleasesWithIncident >= top[1].ReleasesWithIncident);
        Assert.Equal("svc-a", top[0].ServiceName);
    }

    // ── Risk distribution contains all four tiers ─────────────────────────

    [Fact]
    public async Task Handle_MixedRates_DistributionCoversAllTiers()
    {
        // Low: svc-low (1/30 = 3.3%)
        // Medium: svc-med (1/10 = 10%)
        // High: svc-high (2/10 = 20%)
        // Critical: svc-crit (5/10 = 50%)

        var low = Enumerable.Range(1, 30).Select(i => MakeRelease("svc-low", $"1.{i}.0")).ToList();
        var med = Enumerable.Range(1, 10).Select(i => MakeRelease("svc-med", $"1.{i}.0")).ToList();
        var high = Enumerable.Range(1, 10).Select(i => MakeRelease("svc-high", $"1.{i}.0")).ToList();
        var crit = Enumerable.Range(1, 10).Select(i => MakeRelease("svc-crit", $"1.{i}.0")).ToList();

        var incidentMap = new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>();
        incidentMap[low[0].Id] = [MakeIncidentEvent(low[0].Id)];
        incidentMap[med[0].Id] = [MakeIncidentEvent(med[0].Id)];
        foreach (var r in high.Take(2)) incidentMap[r.Id] = [MakeIncidentEvent(r.Id)];
        foreach (var r in crit.Take(5)) incidentMap[r.Id] = [MakeIncidentEvent(r.Id)];

        var all = low.Concat(med).Concat(high).Concat(crit).ToList();
        var handler = CreateHandler(all, incidentMap);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        var dist = result.Value.RiskDistribution;
        Assert.Equal(1, dist.LowCount);
        Assert.Equal(1, dist.MediumCount);
        Assert.Equal(1, dist.HighCount);
        Assert.Equal(1, dist.CriticalCount);
    }

    // ── Validator: TenantId required ──────────────────────────────────────

    [Fact]
    public void Validator_EmptyTenantId_Fails()
    {
        var validator = new GetIncidentChangeCorrelationReport.Validator();
        var result = validator.Validate(new GetIncidentChangeCorrelationReport.Query(TenantId: ""));
        Assert.False(result.IsValid);
    }

    // ── Validator: LookbackDays boundaries ────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public void Validator_LookbackDaysOutOfRange_Fails(int days)
    {
        var validator = new GetIncidentChangeCorrelationReport.Validator();
        var result = validator.Validate(new GetIncidentChangeCorrelationReport.Query(
            TenantId: TenantId.ToString(), LookbackDays: days));
        Assert.False(result.IsValid);
    }

    // ── Validator: valid query passes ─────────────────────────────────────

    [Fact]
    public void Validator_ValidQuery_Passes()
    {
        var validator = new GetIncidentChangeCorrelationReport.Validator();
        var result = validator.Validate(DefaultQuery());
        Assert.True(result.IsValid);
    }

    // ── GeneratedAt matches clock ─────────────────────────────────────────

    [Fact]
    public async Task Handle_GeneratedAt_MatchesClock()
    {
        var handler = CreateHandler([], new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>());
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(FixedNow, result.Value.GeneratedAt);
    }

    // ── LookbackDays echoed in report ─────────────────────────────────────

    [Fact]
    public async Task Handle_LookbackDaysEchoed()
    {
        var handler = CreateHandler([], new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>());
        var query = DefaultQuery() with { LookbackDays = 45 };
        var result = await handler.Handle(query, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(45, result.Value.LookbackDays);
    }

    // ── Environment filter passed to release repo ─────────────────────────

    [Fact]
    public async Task Handle_EnvironmentFilter_PassedToReleaseRepo()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var changeEventRepo = Substitute.For<IChangeEventRepository>();

        releaseRepo.ListInRangeAsync(
                Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
                Arg.Is<string?>(env => env == "staging"),
                Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetIncidentChangeCorrelationReport.Handler(releaseRepo, changeEventRepo, CreateClock());
        var query = DefaultQuery() with { Environment = "staging" };
        var result = await handler.Handle(query, CancellationToken.None);
        Assert.True(result.IsSuccess);

        await releaseRepo.Received(1).ListInRangeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            "staging", Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
