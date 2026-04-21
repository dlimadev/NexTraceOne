using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetOpenDriftImpactSummary;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

using DriftSeverity = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetOpenDriftImpactSummary.GetOpenDriftImpactSummary.DriftSeverity;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para Wave M.3 — GetOpenDriftImpactSummary.
/// Cobre agrupamento por serviço/métrica, distribuição de severidade, métricas mais desviantes
/// e comportamento com lista vazia de drifts.
/// </summary>
public sealed class OpenDriftImpactSummaryTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    /// <summary>
    /// Cria um DriftFinding aberto com desvio derivado dos valores fornecidos.
    /// expectedValue=100, actualValue=100+deviation% aproximado.
    /// </summary>
    private static DriftFinding MakeDrift(
        string service,
        string env,
        string metric,
        decimal deviationPct)
    {
        var expected = 100m;
        var actual = expected + (expected * deviationPct / 100m);
        return DriftFinding.Detect(service, env, metric, expected, actual, FixedNow.AddHours(-1));
    }

    // ── Empty report ───────────────────────────────────────────────────────

    [Fact]
    public async Task Report_Empty_When_No_Open_Drifts()
    {
        var repo = Substitute.For<IDriftFindingRepository>();
        repo.ListUnacknowledgedAsync(1, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        var handler = new GetOpenDriftImpactSummary.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetOpenDriftImpactSummary.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalOpenDrifts.Should().Be(0);
        result.Value.GlobalAvgDeviationPercent.Should().Be(0m);
        result.Value.TopAffectedServices.Should().BeEmpty();
        result.Value.TopDeviantMetrics.Should().BeEmpty();
    }

    // ── Severity distribution ─────────────────────────────────────────────

    [Fact]
    public async Task SeverityDistribution_Correct_For_Mixed_Deviations()
    {
        var drifts = new[]
        {
            MakeDrift("svc-a", "prod", "cpu", 5m),   // Low (<10)
            MakeDrift("svc-a", "prod", "mem", 15m),  // Medium (≥10 <30)
            MakeDrift("svc-b", "prod", "cpu", 40m),  // High (≥30 ≤60)
            MakeDrift("svc-b", "prod", "mem", 80m),  // Critical (>60)
        };
        var repo = Substitute.For<IDriftFindingRepository>();
        repo.ListUnacknowledgedAsync(1, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(drifts);

        var handler = new GetOpenDriftImpactSummary.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetOpenDriftImpactSummary.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SeverityDistribution.LowCount.Should().Be(1);
        result.Value.SeverityDistribution.MediumCount.Should().Be(1);
        result.Value.SeverityDistribution.HighCount.Should().Be(1);
        result.Value.SeverityDistribution.CriticalCount.Should().Be(1);
    }

    // ── Severity boundary tests ───────────────────────────────────────────

    [Theory]
    [InlineData(5.0, "Low")]
    [InlineData(9.9, "Low")]
    [InlineData(10.0, "Medium")]
    [InlineData(29.9, "Medium")]
    [InlineData(30.0, "High")]
    [InlineData(60.0, "High")]
    [InlineData(60.1, "Critical")]
    [InlineData(100.0, "Critical")]
    public async Task WorstSeverity_Classified_Correctly(double deviation, string expectedSeverity)
    {
        var drift = MakeDrift("svc", "prod", "metric", (decimal)deviation);
        var repo = Substitute.For<IDriftFindingRepository>();
        repo.ListUnacknowledgedAsync(1, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([drift]);

        var handler = new GetOpenDriftImpactSummary.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetOpenDriftImpactSummary.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopAffectedServices[0].WorstSeverity.ToString().Should().Be(expectedSeverity);
    }

    // ── Top affected services ─────────────────────────────────────────────

    [Fact]
    public async Task TopAffectedServices_Sorted_By_OpenDriftCount_Descending()
    {
        var drifts = new[]
        {
            MakeDrift("svc-a", "prod", "cpu", 20m),
            MakeDrift("svc-a", "prod", "mem", 20m),
            MakeDrift("svc-a", "prod", "disk", 20m),
            MakeDrift("svc-b", "prod", "cpu", 20m),
        };
        var repo = Substitute.For<IDriftFindingRepository>();
        repo.ListUnacknowledgedAsync(1, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(drifts);

        var handler = new GetOpenDriftImpactSummary.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetOpenDriftImpactSummary.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopAffectedServices[0].ServiceName.Should().Be("svc-a");
        result.Value.TopAffectedServices[0].OpenDriftCount.Should().Be(3);
        result.Value.TopAffectedServices[1].ServiceName.Should().Be("svc-b");
        result.Value.TopAffectedServices[1].OpenDriftCount.Should().Be(1);
    }

    [Fact]
    public async Task TopAffectedServices_Limited_By_MaxServices()
    {
        var drifts = Enumerable.Range(1, 15)
            .Select(i => MakeDrift($"svc-{i:D2}", "prod", "cpu", 20m))
            .ToArray();
        var repo = Substitute.For<IDriftFindingRepository>();
        repo.ListUnacknowledgedAsync(1, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(drifts);

        var handler = new GetOpenDriftImpactSummary.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetOpenDriftImpactSummary.Query(MaxServices: 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopAffectedServices.Should().HaveCount(5);
    }

    // ── Top deviant metrics ───────────────────────────────────────────────

    [Fact]
    public async Task TopDeviantMetrics_Sorted_By_AvgDeviation_Descending()
    {
        var drifts = new[]
        {
            MakeDrift("svc-a", "prod", "latency", 80m),
            MakeDrift("svc-b", "prod", "latency", 60m), // avg=70
            MakeDrift("svc-a", "prod", "cpu", 10m),
            MakeDrift("svc-b", "prod", "cpu", 10m),     // avg=10
        };
        var repo = Substitute.For<IDriftFindingRepository>();
        repo.ListUnacknowledgedAsync(1, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(drifts);

        var handler = new GetOpenDriftImpactSummary.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetOpenDriftImpactSummary.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopDeviantMetrics[0].MetricName.Should().Be("latency");
        result.Value.TopDeviantMetrics[0].AffectedServices.Should().Be(2);
        result.Value.TopDeviantMetrics[1].MetricName.Should().Be("cpu");
    }

    [Fact]
    public async Task TopDeviantMetrics_Limited_By_MaxMetrics()
    {
        var metrics = Enumerable.Range(1, 20)
            .Select(i => MakeDrift("svc", "prod", $"metric-{i:D2}", 20m))
            .ToArray();
        var repo = Substitute.For<IDriftFindingRepository>();
        repo.ListUnacknowledgedAsync(1, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(metrics);

        var handler = new GetOpenDriftImpactSummary.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetOpenDriftImpactSummary.Query(MaxMetrics: 7), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopDeviantMetrics.Should().HaveCount(7);
    }

    // ── Global metrics ────────────────────────────────────────────────────

    [Fact]
    public async Task GlobalAvgDeviation_Computed_Correctly()
    {
        var drifts = new[]
        {
            MakeDrift("svc", "prod", "cpu", 20m),
            MakeDrift("svc", "prod", "mem", 40m),
        };
        var repo = Substitute.For<IDriftFindingRepository>();
        repo.ListUnacknowledgedAsync(1, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(drifts);

        var handler = new GetOpenDriftImpactSummary.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetOpenDriftImpactSummary.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GlobalAvgDeviationPercent.Should().Be(30.0m); // (20+40)/2
    }

    [Fact]
    public async Task GlobalMaxDeviation_Is_Largest_Deviation()
    {
        var drifts = new[]
        {
            MakeDrift("svc", "prod", "cpu", 15m),
            MakeDrift("svc", "prod", "mem", 75m),
            MakeDrift("svc", "prod", "disk", 30m),
        };
        var repo = Substitute.For<IDriftFindingRepository>();
        repo.ListUnacknowledgedAsync(1, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(drifts);

        var handler = new GetOpenDriftImpactSummary.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetOpenDriftImpactSummary.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GlobalMaxDeviationPercent.Should().Be(75.0m);
    }

    // ── Acknowledged drifts excluded ──────────────────────────────────────

    [Fact]
    public async Task Acknowledged_Drifts_Excluded_From_Report()
    {
        // Detect + acknowledge
        var ackDrift = DriftFinding.Detect("svc-ack", "prod", "cpu", 100m, 180m, FixedNow.AddHours(-2));
        ackDrift.Acknowledge();

        var openDrift = MakeDrift("svc-open", "prod", "cpu", 20m);

        var repo = Substitute.For<IDriftFindingRepository>();
        repo.ListUnacknowledgedAsync(1, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { ackDrift, openDrift });

        var handler = new GetOpenDriftImpactSummary.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetOpenDriftImpactSummary.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalOpenDrifts.Should().Be(1);
        result.Value.TopAffectedServices.Should().ContainSingle(s => s.ServiceName == "svc-open");
    }

    // ── GeneratedAt ───────────────────────────────────────────────────────

    [Fact]
    public async Task Report_GeneratedAt_Uses_Clock()
    {
        var repo = Substitute.For<IDriftFindingRepository>();
        repo.ListUnacknowledgedAsync(1, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        var handler = new GetOpenDriftImpactSummary.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetOpenDriftImpactSummary.Query(), CancellationToken.None);

        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    // ── Validation ────────────────────────────────────────────────────────

    [Fact]
    public void Validator_Rejects_MaxServices_Zero()
    {
        var v = new GetOpenDriftImpactSummary.Validator();
        v.Validate(new GetOpenDriftImpactSummary.Query(MaxServices: 0)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Rejects_MaxMetrics_Over_50()
    {
        var v = new GetOpenDriftImpactSummary.Validator();
        v.Validate(new GetOpenDriftImpactSummary.Query(MaxMetrics: 51)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Accepts_Valid_Query()
    {
        var v = new GetOpenDriftImpactSummary.Validator();
        v.Validate(new GetOpenDriftImpactSummary.Query()).IsValid.Should().BeTrue();
    }
}
