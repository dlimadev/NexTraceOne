using NexTraceOne.RuntimeIntelligence.Domain.Entities;

namespace NexTraceOne.RuntimeIntelligence.Tests.Domain.Entities;

/// <summary>Testes unitários da entidade RuntimeBaseline — tolerância, confiança e refresh.</summary>
public sealed class RuntimeBaselineTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static RuntimeBaseline CreateBaseline() =>
        RuntimeBaseline.Establish("OrderSvc", "prod", 50m, 200m, 0.02m, 500m, FixedNow, 100, 0.9m);

    // ── Establish ─────────────────────────────────────────────────────────

    [Fact]
    public void Establish_WithValidData_ShouldSetProperties()
    {
        var baseline = CreateBaseline();

        baseline.ServiceName.Should().Be("OrderSvc");
        baseline.ExpectedAvgLatencyMs.Should().Be(50m);
        baseline.ExpectedErrorRate.Should().Be(0.02m);
        baseline.ConfidenceScore.Should().Be(0.9m);
        baseline.DataPointCount.Should().Be(100);
        baseline.IsConfident.Should().BeTrue();
    }

    [Fact]
    public void Establish_WithLowConfidence_ShouldNotBeConfident()
    {
        var baseline = RuntimeBaseline.Establish("Svc", "dev", 50m, 200m, 0.02m, 500m, FixedNow, 5, 0.3m);

        baseline.IsConfident.Should().BeFalse();
    }

    [Fact]
    public void Establish_ShouldClampConfidenceScore()
    {
        var baseline = RuntimeBaseline.Establish("Svc", "prod", 50m, 200m, 0.02m, 500m, FixedNow, 100, 1.5m);

        baseline.ConfidenceScore.Should().Be(1m);
    }

    [Fact]
    public void Establish_ShouldClampErrorRate()
    {
        var baseline = RuntimeBaseline.Establish("Svc", "prod", 50m, 200m, 1.5m, 500m, FixedNow, 100, 0.9m);

        baseline.ExpectedErrorRate.Should().Be(1m);
    }

    // ── IsWithinTolerance ─────────────────────────────────────────────────

    [Fact]
    public void IsWithinTolerance_WhenAllMetricsInRange_ShouldReturnTrue()
    {
        var baseline = CreateBaseline();
        var snapshot = RuntimeSnapshot.Create("OrderSvc", "prod", 55m, 210m, 0.022m, 480m, 40m, 512m, 3, FixedNow, "Prom");

        baseline.IsWithinTolerance(snapshot, 20m).Should().BeTrue();
    }

    [Fact]
    public void IsWithinTolerance_WhenLatencyExceedsTolerance_ShouldReturnFalse()
    {
        var baseline = CreateBaseline();
        var snapshot = RuntimeSnapshot.Create("OrderSvc", "prod", 100m, 400m, 0.02m, 500m, 40m, 512m, 3, FixedNow, "Prom");

        baseline.IsWithinTolerance(snapshot, 20m).Should().BeFalse();
    }

    [Fact]
    public void IsWithinTolerance_WithZeroTolerance_ShouldReturnFalse()
    {
        var baseline = CreateBaseline();
        var snapshot = RuntimeSnapshot.Create("OrderSvc", "prod", 50m, 200m, 0.02m, 500m, 40m, 512m, 3, FixedNow, "Prom");

        baseline.IsWithinTolerance(snapshot, 0m).Should().BeFalse();
    }

    // ── Refresh ───────────────────────────────────────────────────────────

    [Fact]
    public void Refresh_ShouldUpdateAllValues()
    {
        var baseline = CreateBaseline();
        var refreshedAt = FixedNow.AddDays(30);

        baseline.Refresh(60m, 250m, 0.03m, 600m, refreshedAt, 200, 0.95m);

        baseline.ExpectedAvgLatencyMs.Should().Be(60m);
        baseline.ExpectedP99LatencyMs.Should().Be(250m);
        baseline.ExpectedErrorRate.Should().Be(0.03m);
        baseline.ExpectedRequestsPerSecond.Should().Be(600m);
        baseline.EstablishedAt.Should().Be(refreshedAt);
        baseline.DataPointCount.Should().Be(200);
        baseline.ConfidenceScore.Should().Be(0.95m);
    }
}
