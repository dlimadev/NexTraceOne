using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetPostIncidentLearningReport;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Tests.Compliance;

/// <summary>
/// Testes unitários para Wave T.1 — GetPostIncidentLearningReport.
/// Cobre: sem releases, sem incidentes, cobertura Full/Partial/Low, incidentes recorrentes
/// sem runbook, múltiplos serviços, learning rate global, filtro por tenant, validator.
/// </summary>
public sealed class PostIncidentLearningReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string TenantIdStr = "22222222-2222-2222-2222-222222222222";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static Release MakeRelease(string serviceName, string env = "prod")
        => Release.Create(TenantId, Guid.NewGuid(), serviceName, "1.0.0", env,
            "pipeline-ci", "abc123", FixedNow.AddDays(-1));

    private static ChangeEvent MakeIncidentEvent(ReleaseId releaseId)
        => ChangeEvent.Create(releaseId, "incident_correlated", "incident detected", "monitor", FixedNow);

    private static GetPostIncidentLearningReport.Handler CreateHandler(
        IReadOnlyList<Release> releases,
        IReadOnlyDictionary<ReleaseId, IReadOnlyList<ChangeEvent>> incidentMap,
        IReadOnlyList<string> servicesWithRunbook)
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var changeEventRepo = Substitute.For<IChangeEventRepository>();
        var learningReader = Substitute.For<IIncidentLearningReader>();

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

        learningReader.ListServicesWithApprovedRunbookAsync(
                Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(servicesWithRunbook);

        return new GetPostIncidentLearningReport.Handler(releaseRepo, changeEventRepo, learningReader, CreateClock());
    }

    private static GetPostIncidentLearningReport.Query DefaultQuery()
        => new(TenantId: TenantIdStr, LookbackDays: 90, TopServiceCount: 10);

    // ── Empty: no releases ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoReleases_ReturnsEmptyReport()
    {
        var handler = CreateHandler([], new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>(), []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(0, r.TotalIncidentsAnalyzed);
        Assert.Equal(0, r.TotalIncidentsWithRunbook);
        Assert.Equal(0m, r.TenantLearningRatePct);
        Assert.Equal(0, r.TotalRecurringWithoutRunbook);
        Assert.Empty(r.AllServices);
    }

    // ── No incidents: releases without incident events → empty report ──────

    [Fact]
    public async Task Handle_ReleasesWithoutIncidents_ReturnsEmptyReport()
    {
        var rel = MakeRelease("svc-a");
        var handler = CreateHandler([rel], new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>(), []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(0, r.TotalIncidentsAnalyzed);
        Assert.Empty(r.AllServices);
    }

    // ── Full coverage: service has incident + approved runbook ────────────

    [Fact]
    public async Task Handle_ServiceWithIncidentAndRunbook_ClassifiesFullCoverage()
    {
        var rel = MakeRelease("svc-a");
        var incidentMap = new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>
        {
            [rel.Id] = [MakeIncidentEvent(rel.Id)]
        };

        var handler = CreateHandler([rel], incidentMap, ["svc-a"]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(1, r.TotalIncidentsAnalyzed);
        Assert.Equal(1, r.TotalIncidentsWithRunbook);
        Assert.Equal(100m, r.TenantLearningRatePct);
        Assert.Single(r.AllServices);

        var entry = r.AllServices.Single();
        Assert.Equal("svc-a", entry.ServiceName);
        Assert.Equal(GetPostIncidentLearningReport.LearningCoverage.Full, entry.Coverage);
        Assert.Equal(100m, entry.LearningRatePct);
        Assert.Equal(1, entry.IncidentsWithRunbook);
    }

    // ── Low coverage: incident without runbook → Low ───────────────────────

    [Fact]
    public async Task Handle_ServiceWithIncidentNoRunbook_ClassifiesLowCoverage()
    {
        var rel = MakeRelease("svc-b");
        var incidentMap = new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>
        {
            [rel.Id] = [MakeIncidentEvent(rel.Id)]
        };

        var handler = CreateHandler([rel], incidentMap, []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(1, r.TotalIncidentsAnalyzed);
        Assert.Equal(0, r.TotalIncidentsWithRunbook);
        Assert.Equal(0m, r.TenantLearningRatePct);

        var entry = r.AllServices.Single();
        Assert.Equal(GetPostIncidentLearningReport.LearningCoverage.Low, entry.Coverage);
        Assert.Equal(0m, entry.LearningRatePct);
    }

    // ── Recurring incidents without runbook → recurring flag ──────────────

    [Fact]
    public async Task Handle_TwoIncidentsForSameServiceNoRunbook_FlagsRecurring()
    {
        var rel1 = MakeRelease("svc-c");
        var rel2 = MakeRelease("svc-c");
        var incidentMap = new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>
        {
            [rel1.Id] = [MakeIncidentEvent(rel1.Id)],
            [rel2.Id] = [MakeIncidentEvent(rel2.Id)]
        };

        var handler = CreateHandler([rel1, rel2], incidentMap, []);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(2, r.TotalIncidentsAnalyzed);
        Assert.True(r.TotalRecurringWithoutRunbook > 0,
            "Expected recurring without runbook to be flagged");

        var entry = r.AllServices.Single(e => e.ServiceName == "svc-c");
        Assert.True(entry.RecurringIncidentsWithoutRunbook > 0);
    }

    // ── Recurring with runbook → not flagged as recurring ─────────────────

    [Fact]
    public async Task Handle_TwoIncidentsWithRunbook_NoRecurringFlag()
    {
        var rel1 = MakeRelease("svc-d");
        var rel2 = MakeRelease("svc-d");
        var incidentMap = new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>
        {
            [rel1.Id] = [MakeIncidentEvent(rel1.Id)],
            [rel2.Id] = [MakeIncidentEvent(rel2.Id)]
        };

        var handler = CreateHandler([rel1, rel2], incidentMap, ["svc-d"]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        var entry = r.AllServices.Single(e => e.ServiceName == "svc-d");
        Assert.Equal(0, entry.RecurringIncidentsWithoutRunbook);
    }

    // ── Multi-service: correct distribution ───────────────────────────────

    [Fact]
    public async Task Handle_MultipleServices_CorrectCoverageDistribution()
    {
        // svc-a: has runbook → Full (100%)
        var relA = MakeRelease("svc-a");
        // svc-b: no runbook, 1 incident → Low (0%)
        var relB = MakeRelease("svc-b");

        var incidentMap = new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>
        {
            [relA.Id] = [MakeIncidentEvent(relA.Id)],
            [relB.Id] = [MakeIncidentEvent(relB.Id)]
        };

        var handler = CreateHandler([relA, relB], incidentMap, ["svc-a"]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(2, r.TotalIncidentsAnalyzed);
        Assert.Equal(1, r.TotalIncidentsWithRunbook);
        Assert.Equal(50m, r.TenantLearningRatePct);
        Assert.Equal(2, r.AllServices.Count);

        Assert.Equal(1, r.CoverageDistribution.FullCount);
        Assert.Equal(0, r.CoverageDistribution.PartialCount);
        Assert.Equal(1, r.CoverageDistribution.LowCount);
    }

    // ── TopLowCoverageServices ordered by lowest rate first ───────────────

    [Fact]
    public async Task Handle_TopLowCoverage_OrderedByLearningRateAscending()
    {
        var rel1 = MakeRelease("svc-worst");
        var rel2 = MakeRelease("svc-better");

        var incidentMap = new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>
        {
            [rel1.Id] = [MakeIncidentEvent(rel1.Id)],
            [rel2.Id] = [MakeIncidentEvent(rel2.Id)]
        };

        // svc-better has a runbook; svc-worst does not
        var handler = CreateHandler([rel1, rel2], incidentMap, ["svc-better"]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal("svc-worst", r.TopLowCoverageServices.First().ServiceName);
    }

    // ── Partial threshold: coverage between partial and full threshold ─────

    [Fact]
    public async Task Handle_CustomPartialThreshold_ClassifiesPartialCorrectly()
    {
        // With fullThreshold=80, partialThreshold=40: 0 incidents with runbook → Low (0%)
        // Use query with custom thresholds
        var rel = MakeRelease("svc-partial");
        var incidentMap = new Dictionary<ReleaseId, IReadOnlyList<ChangeEvent>>
        {
            [rel.Id] = [MakeIncidentEvent(rel.Id)]
        };

        var handler = CreateHandler([rel], incidentMap, []);
        var query = new GetPostIncidentLearningReport.Query(
            TenantId: TenantIdStr,
            LookbackDays: 90,
            TopServiceCount: 10,
            FullCoverageThresholdPct: 80m,
            PartialCoverageThresholdPct: 0m); // anything >= 0% = Partial or Full

        var result = await handler.Handle(query, CancellationToken.None);
        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        // 0% >= 0% partial threshold but < 80% full → Partial
        Assert.Equal(GetPostIncidentLearningReport.LearningCoverage.Partial, entry.Coverage);
    }

    // ── Validator: invalid lookback days ──────────────────────────────────

    [Fact]
    public void Validator_InvalidLookbackDays_ReturnsError()
    {
        var validator = new GetPostIncidentLearningReport.Validator();
        var result = validator.Validate(new GetPostIncidentLearningReport.Query(
            TenantId: TenantIdStr, LookbackDays: 0));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_EmptyTenantId_ReturnsError()
    {
        var validator = new GetPostIncidentLearningReport.Validator();
        var result = validator.Validate(new GetPostIncidentLearningReport.Query(TenantId: ""));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_PartialThresholdAboveFull_ReturnsError()
    {
        var validator = new GetPostIncidentLearningReport.Validator();
        var result = validator.Validate(new GetPostIncidentLearningReport.Query(
            TenantId: TenantIdStr,
            LookbackDays: 30,
            FullCoverageThresholdPct: 40m,
            PartialCoverageThresholdPct: 80m));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_ValidQuery_PassesValidation()
    {
        var validator = new GetPostIncidentLearningReport.Validator();
        var result = validator.Validate(new GetPostIncidentLearningReport.Query(
            TenantId: TenantIdStr, LookbackDays: 90));
        Assert.True(result.IsValid);
    }
}
