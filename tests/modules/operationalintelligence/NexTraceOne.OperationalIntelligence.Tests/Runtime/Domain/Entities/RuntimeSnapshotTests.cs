using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Domain.Entities;

/// <summary>Testes unitários da entidade RuntimeSnapshot — classificação de saúde e desvios.</summary>
public sealed class RuntimeSnapshotTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static RuntimeSnapshot CreateHealthy() =>
        RuntimeSnapshot.Create("OrderSvc", "prod", 50m, 200m, 0.01m, 500m, 40m, 512m, 3, FixedNow, "Prometheus");

    // ── ClassifyHealth (encapsulado) ──────────────────────────────────────

    [Fact]
    public void Create_WithLowErrorAndLatency_ShouldBeHealthy()
    {
        var snapshot = RuntimeSnapshot.Create("Svc", "prod", 50m, 200m, 0.02m, 500m, 40m, 512m, 3, FixedNow, "Prometheus");

        snapshot.HealthStatus.Should().Be(HealthStatus.Healthy);
        snapshot.IsHealthy.Should().BeTrue();
        snapshot.IsDegraded.Should().BeFalse();
        snapshot.IsUnhealthy.Should().BeFalse();
    }

    [Fact]
    public void Create_WithHighErrorRate_ShouldBeDegraded()
    {
        var snapshot = RuntimeSnapshot.Create("Svc", "prod", 50m, 200m, 0.06m, 500m, 40m, 512m, 3, FixedNow, "Prometheus");

        snapshot.HealthStatus.Should().Be(HealthStatus.Degraded);
        snapshot.IsDegraded.Should().BeTrue();
    }

    [Fact]
    public void Create_WithVeryHighErrorRate_ShouldBeUnhealthy()
    {
        var snapshot = RuntimeSnapshot.Create("Svc", "prod", 50m, 200m, 0.15m, 500m, 40m, 512m, 3, FixedNow, "Prometheus");

        snapshot.HealthStatus.Should().Be(HealthStatus.Unhealthy);
        snapshot.IsUnhealthy.Should().BeTrue();
    }

    [Fact]
    public void Create_WithHighLatency_ShouldBeDegraded()
    {
        var snapshot = RuntimeSnapshot.Create("Svc", "prod", 500m, 1500m, 0.01m, 500m, 40m, 512m, 3, FixedNow, "Prometheus");

        snapshot.HealthStatus.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public void Create_WithVeryHighLatency_ShouldBeUnhealthy()
    {
        var snapshot = RuntimeSnapshot.Create("Svc", "prod", 2000m, 3500m, 0.01m, 500m, 40m, 512m, 3, FixedNow, "Prometheus");

        snapshot.HealthStatus.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public void Create_ShouldClampErrorRate()
    {
        var snapshot = RuntimeSnapshot.Create("Svc", "prod", 50m, 200m, 1.5m, 500m, 40m, 512m, 3, FixedNow, "Prom");

        snapshot.ErrorRate.Should().Be(1m);
    }

    [Fact]
    public void Create_ShouldClampCpuUsage()
    {
        var snapshot = RuntimeSnapshot.Create("Svc", "prod", 50m, 200m, 0.01m, 500m, 150m, 512m, 3, FixedNow, "Prom");

        snapshot.CpuUsagePercent.Should().Be(100m);
    }

    // ── CalculateDeviationsFrom ───────────────────────────────────────────

    [Fact]
    public void CalculateDeviationsFrom_ShouldComputeCorrectly()
    {
        var baseline = RuntimeBaseline.Establish("Svc", "prod", 50m, 200m, 0.02m, 500m, FixedNow, 100, 0.9m);
        var snapshot = RuntimeSnapshot.Create("Svc", "prod", 75m, 300m, 0.04m, 400m, 40m, 512m, 3, FixedNow, "Prom");

        var deviations = snapshot.CalculateDeviationsFrom(baseline);

        deviations.Should().ContainKey("AvgLatencyMs");
        deviations["AvgLatencyMs"].Should().Be(50m);
        deviations.Should().ContainKey("P99LatencyMs");
        deviations["P99LatencyMs"].Should().Be(50m);
        deviations.Should().ContainKey("ErrorRate");
        deviations["ErrorRate"].Should().Be(100m);
        deviations.Should().ContainKey("RequestsPerSecond");
        deviations["RequestsPerSecond"].Should().Be(-20m);
    }
}
