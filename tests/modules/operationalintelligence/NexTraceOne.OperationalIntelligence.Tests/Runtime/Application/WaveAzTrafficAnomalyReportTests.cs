using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetTrafficAnomalyReport;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para Wave AZ.3 GetTrafficAnomalyReport (~16 testes).
/// </summary>
public sealed class WaveAzTrafficAnomalyReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-az-003";

    private static IDateTimeProvider Clock()
    {
        var c = Substitute.For<IDateTimeProvider>();
        c.UtcNow.Returns(FixedNow);
        return c;
    }

    private static GetTrafficAnomalyReport.Handler BuildHandler(
        ITrafficAnomalyReader? reader = null) =>
        new(reader ?? Substitute.For<ITrafficAnomalyReader>(), Clock());

    private static ITrafficAnomalyReader BuildReader(
        IReadOnlyList<ITrafficAnomalyReader.ServiceTrafficAnomalyEntry> entries,
        IReadOnlyList<ITrafficAnomalyReader.TimelineEvent>? timeline = null)
    {
        var r = Substitute.For<ITrafficAnomalyReader>();
        r.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
         .Returns(entries);
        r.GetTimelineEventsAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
         .Returns(timeline ?? []);
        return r;
    }

    private static ITrafficAnomalyReader.ServiceTrafficAnomalyEntry MakeSvc(
        string svcId = "svc-1",
        string svcName = "svc-1",
        IReadOnlyList<ITrafficAnomalyReader.AnomalyObservation>? anomalies = null,
        double baselineRps = 50.0,
        double baselineErrorRate = 1.0,
        double baselineLatency = 200.0) =>
        new(svcId, svcName, "team-1", anomalies ?? [], baselineRps, baselineErrorRate, baselineLatency);

    private static ITrafficAnomalyReader.AnomalyObservation MakeAnomaly(
        string anomalyType = "SpikeAnomaly",
        string correlation = "Unexplained",
        string? eventId = null,
        DateTimeOffset? detectedAt = null,
        DateTimeOffset? resolvedAt = null,
        double observed = 500.0,
        double baseline = 50.0) =>
        new(anomalyType,
            detectedAt ?? FixedNow.AddHours(-2),
            resolvedAt,
            observed, baseline,
            correlation, eventId);

    // ────────────────────────────────────────────────────────────────────────
    // Empty report
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AZ3_EmptyReport_WhenNoEntries()
    {
        var h = BuildHandler(BuildReader([]));
        var r = await h.Handle(new GetTrafficAnomalyReport.Query(TenantId), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value.Anomalies.Should().BeEmpty();
        r.Value.Summary.TotalAnomalies.Should().Be(0);
        r.Value.Summary.AnomalyResolutionRate.Should().Be(100m);
    }

    // ────────────────────────────────────────────────────────────────────────
    // AnomalyCorrelation parsing
    // ────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("CorrelatedWithDeploy", GetTrafficAnomalyReport.AnomalyCorrelation.CorrelatedWithDeploy)]
    [InlineData("CorrelatedWithIncident", GetTrafficAnomalyReport.AnomalyCorrelation.CorrelatedWithIncident)]
    [InlineData("Unexplained", GetTrafficAnomalyReport.AnomalyCorrelation.Unexplained)]
    [InlineData("Unknown", GetTrafficAnomalyReport.AnomalyCorrelation.Unexplained)]
    public async Task AZ3_AnomalyCorrelation_ParsedCorrectly(
        string rawCorrelation, GetTrafficAnomalyReport.AnomalyCorrelation expected)
    {
        var svc = MakeSvc(anomalies: [MakeAnomaly(correlation: rawCorrelation)]);
        var h = BuildHandler(BuildReader([svc]));
        var r = await h.Handle(new GetTrafficAnomalyReport.Query(TenantId), CancellationToken.None);

        r.Value.Anomalies[0].Correlation.Should().Be(expected);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Severity classification
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AZ3_Severity_Critical_ForMassiveErrorRateSpike()
    {
        // baselineErrorRate=1, observed=10 → 10 > 1*5=5 → Critical
        var svc = MakeSvc(
            baselineErrorRate: 1.0,
            anomalies: [MakeAnomaly("ErrorRateSpike", observed: 10.0, baseline: 1.0)]);

        var h = BuildHandler(BuildReader([svc]));
        var r = await h.Handle(new GetTrafficAnomalyReport.Query(TenantId), CancellationToken.None);

        r.Value.Anomalies[0].Severity.Should().Be(GetTrafficAnomalyReport.AnomalySeverity.Critical);
    }

    [Fact]
    public async Task AZ3_Severity_Warning_ForModerateErrorRateSpike()
    {
        // observed=2 < baseline*5=5 → Warning
        var svc = MakeSvc(
            baselineErrorRate: 1.0,
            anomalies: [MakeAnomaly("ErrorRateSpike", observed: 2.0, baseline: 1.0)]);

        var h = BuildHandler(BuildReader([svc]));
        var r = await h.Handle(new GetTrafficAnomalyReport.Query(TenantId), CancellationToken.None);

        r.Value.Anomalies[0].Severity.Should().Be(GetTrafficAnomalyReport.AnomalySeverity.Warning);
    }

    [Fact]
    public async Task AZ3_Severity_Critical_ForMassiveSpikeAnomaly()
    {
        // observed=500 > baseline=50 × 5=250 → Critical
        var svc = MakeSvc(anomalies: [MakeAnomaly("SpikeAnomaly", observed: 500.0, baseline: 50.0)]);
        var h = BuildHandler(BuildReader([svc]));
        var r = await h.Handle(new GetTrafficAnomalyReport.Query(TenantId), CancellationToken.None);

        r.Value.Anomalies[0].Severity.Should().Be(GetTrafficAnomalyReport.AnomalySeverity.Critical);
    }

    [Fact]
    public async Task AZ3_Severity_Critical_ForNearZeroDropAnomaly()
    {
        // observed=1 < baseline=50 × 0.1=5 → Critical
        var svc = MakeSvc(anomalies: [MakeAnomaly("DropAnomaly", observed: 1.0, baseline: 50.0)]);
        var h = BuildHandler(BuildReader([svc]));
        var r = await h.Handle(new GetTrafficAnomalyReport.Query(TenantId), CancellationToken.None);

        r.Value.Anomalies[0].Severity.Should().Be(GetTrafficAnomalyReport.AnomalySeverity.Critical);
    }

    // ────────────────────────────────────────────────────────────────────────
    // UnexplainedAnomalyList
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AZ3_UnexplainedList_ContainsOnlyUnexplained()
    {
        var svc = MakeSvc(anomalies: [
            MakeAnomaly(correlation: "Unexplained"),
            MakeAnomaly(correlation: "CorrelatedWithDeploy", observed: 100.0, baseline: 50.0)
        ]);

        var h = BuildHandler(BuildReader([svc]));
        var r = await h.Handle(new GetTrafficAnomalyReport.Query(TenantId), CancellationToken.None);

        r.Value.UnexplainedAnomalyList.Should().HaveCount(1);
        r.Value.UnexplainedAnomalyList[0].Correlation
            .Should().Be(GetTrafficAnomalyReport.AnomalyCorrelation.Unexplained);
    }

    // ────────────────────────────────────────────────────────────────────────
    // AnomalyResolutionRate
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AZ3_ResolutionRate_100_WhenAllResolved_WithoutIncident()
    {
        var resolved = FixedNow.AddHours(-1);
        var svc = MakeSvc(anomalies: [
            MakeAnomaly(correlation: "Unexplained", resolvedAt: resolved),
            MakeAnomaly(correlation: "CorrelatedWithDeploy", resolvedAt: resolved, observed: 100, baseline: 50)
        ]);

        var h = BuildHandler(BuildReader([svc]));
        var r = await h.Handle(new GetTrafficAnomalyReport.Query(TenantId), CancellationToken.None);

        r.Value.Summary.AnomalyResolutionRate.Should().Be(100m);
    }

    [Fact]
    public async Task AZ3_ResolutionRate_0_WhenAllCorrelatedWithIncident()
    {
        var resolved = FixedNow.AddHours(-1);
        var svc = MakeSvc(anomalies: [
            MakeAnomaly(correlation: "CorrelatedWithIncident"),
            MakeAnomaly(correlation: "CorrelatedWithIncident", observed: 200, baseline: 50)
        ]);

        var h = BuildHandler(BuildReader([svc]));
        var r = await h.Handle(new GetTrafficAnomalyReport.Query(TenantId), CancellationToken.None);

        r.Value.Summary.AnomalyResolutionRate.Should().Be(0m);
    }

    // ────────────────────────────────────────────────────────────────────────
    // AnomalyTimeline
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AZ3_Timeline_MergesAnomaliesAndExternalEvents()
    {
        var svc = MakeSvc(anomalies: [MakeAnomaly(detectedAt: FixedNow.AddHours(-2))]);
        var events = new[]
        {
            new ITrafficAnomalyReader.TimelineEvent(FixedNow.AddHours(-3), "Deploy", "rel-001", "v1.2 deployed")
        };

        var h = BuildHandler(BuildReader([svc], events));
        var r = await h.Handle(new GetTrafficAnomalyReport.Query(TenantId), CancellationToken.None);

        r.Value.AnomalyTimeline.Should().HaveCount(2);
        r.Value.AnomalyTimeline[0].Timestamp.Should().BeBefore(r.Value.AnomalyTimeline[1].Timestamp);
    }

    // ────────────────────────────────────────────────────────────────────────
    // RecurringAnomalyPatterns
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AZ3_RecurringPatterns_Detected_WhenSameAnomalyThreeTimes()
    {
        var monday = new DateTimeOffset(2026, 9, 28, 10, 0, 0, TimeSpan.Zero); // Monday
        var anomalies = Enumerable.Range(0, 3)
            .Select(i => MakeAnomaly("ErrorRateSpike",
                detectedAt: monday.AddDays(i * 7),
                observed: 20.0, baseline: 1.0))
            .ToList<ITrafficAnomalyReader.AnomalyObservation>();

        var svc = MakeSvc(anomalies: anomalies);
        var h = BuildHandler(BuildReader([svc]));
        var r = await h.Handle(new GetTrafficAnomalyReport.Query(TenantId), CancellationToken.None);

        r.Value.RecurringAnomalyPatterns.Should().NotBeEmpty();
        r.Value.RecurringAnomalyPatterns[0].AnomalyType.Should().Be("ErrorRateSpike");
    }

    // ────────────────────────────────────────────────────────────────────────
    // NullTrafficAnomalyReader
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AZ3_NullReader_ReturnsEmptyReport()
    {
        var h = new GetTrafficAnomalyReport.Handler(
            new NexTraceOne.OperationalIntelligence.Application.Runtime.NullTrafficAnomalyReader(),
            Clock());

        var r = await h.Handle(new GetTrafficAnomalyReport.Query(TenantId), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value.Anomalies.Should().BeEmpty();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Validator
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AZ3_Validator_RejectsEmptyTenantId()
    {
        var v = new GetTrafficAnomalyReport.Validator();
        v.Validate(new GetTrafficAnomalyReport.Query("")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AZ3_Validator_RejectsInvalidDropPct()
    {
        var v = new GetTrafficAnomalyReport.Validator();
        v.Validate(new GetTrafficAnomalyReport.Query("t", DropPct: 0)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AZ3_Validator_RejectsInvalidLookbackDays()
    {
        var v = new GetTrafficAnomalyReport.Validator();
        v.Validate(new GetTrafficAnomalyReport.Query("t", LookbackDays: 0)).IsValid.Should().BeFalse();
    }
}
