using FluentAssertions;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Tests.Telemetry;

/// <summary>
/// Testes dos modelos do Product Store: ServiceMetricsSnapshot, DependencyMetricsSnapshot.
/// Valida: criação, campos obrigatórios, valores default, cálculos derivados.
/// </summary>
public sealed class ServiceMetricsSnapshotTests
{
    [Fact]
    public void Create_WithRequiredFields_ShouldSucceed()
    {
        var snapshot = new ServiceMetricsSnapshot
        {
            ServiceId = Guid.NewGuid(),
            ServiceName = "payment-api",
            Environment = "production",
            AggregationLevel = AggregationLevel.OneMinute,
            IntervalStart = DateTimeOffset.UtcNow.AddMinutes(-1),
            IntervalEnd = DateTimeOffset.UtcNow,
            RequestCount = 1500,
            RequestsPerMinute = 1500,
            ErrorCount = 3,
            ErrorRatePercent = 0.2,
            LatencyAvgMs = 45.2,
            LatencyP50Ms = 32.0,
            LatencyP95Ms = 120.5,
            LatencyP99Ms = 350.0,
            LatencyMaxMs = 1200.0
        };

        snapshot.ServiceName.Should().Be("payment-api");
        snapshot.AggregationLevel.Should().Be(AggregationLevel.OneMinute);
        snapshot.RequestCount.Should().Be(1500);
        snapshot.ErrorRatePercent.Should().BeApproximately(0.2, 0.01);
        snapshot.LatencyP95Ms.Should().Be(120.5);
        snapshot.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithTenantId_ShouldSupportMultiTenancy()
    {
        var tenantId = Guid.NewGuid();
        var snapshot = new ServiceMetricsSnapshot
        {
            ServiceId = Guid.NewGuid(),
            ServiceName = "accounts-api",
            Environment = "production",
            AggregationLevel = AggregationLevel.OneHour,
            IntervalStart = DateTimeOffset.UtcNow.AddHours(-1),
            IntervalEnd = DateTimeOffset.UtcNow,
            TenantId = tenantId
        };

        snapshot.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_WithResourceMetrics_ShouldTrackCpuAndMemory()
    {
        var snapshot = new ServiceMetricsSnapshot
        {
            ServiceId = Guid.NewGuid(),
            ServiceName = "order-api",
            Environment = "production",
            AggregationLevel = AggregationLevel.OneMinute,
            IntervalStart = DateTimeOffset.UtcNow.AddMinutes(-1),
            IntervalEnd = DateTimeOffset.UtcNow,
            CpuAvgPercent = 45.5,
            MemoryAvgMb = 512.0
        };

        snapshot.CpuAvgPercent.Should().Be(45.5);
        snapshot.MemoryAvgMb.Should().Be(512.0);
    }

    [Fact]
    public void DependencyMetrics_ShouldTrackSourceAndTarget()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var snapshot = new DependencyMetricsSnapshot
        {
            SourceServiceId = sourceId,
            SourceServiceName = "order-api",
            TargetServiceId = targetId,
            TargetServiceName = "payment-api",
            Environment = "production",
            AggregationLevel = AggregationLevel.OneMinute,
            IntervalStart = DateTimeOffset.UtcNow.AddMinutes(-1),
            IntervalEnd = DateTimeOffset.UtcNow,
            CallCount = 500,
            ErrorCount = 2,
            ErrorRatePercent = 0.4,
            LatencyAvgMs = 35.0,
            LatencyP95Ms = 80.0,
            LatencyP99Ms = 200.0
        };

        snapshot.SourceServiceName.Should().Be("order-api");
        snapshot.TargetServiceName.Should().Be("payment-api");
        snapshot.CallCount.Should().Be(500);
        snapshot.ErrorRatePercent.Should().BeApproximately(0.4, 0.01);
    }

    [Fact]
    public void AggregationLevel_ShouldDistinguishGranularities()
    {
        AggregationLevel.Raw.Should().NotBe(AggregationLevel.OneMinute);
        AggregationLevel.OneMinute.Should().NotBe(AggregationLevel.OneHour);
        AggregationLevel.OneHour.Should().NotBe(AggregationLevel.OneDay);
    }

    [Fact]
    public void DefaultId_ShouldBeUniquePerSnapshot()
    {
        var s1 = new ServiceMetricsSnapshot
        {
            ServiceId = Guid.NewGuid(),
            ServiceName = "api",
            Environment = "prod",
            AggregationLevel = AggregationLevel.OneMinute,
            IntervalStart = DateTimeOffset.UtcNow,
            IntervalEnd = DateTimeOffset.UtcNow
        };

        var s2 = new ServiceMetricsSnapshot
        {
            ServiceId = Guid.NewGuid(),
            ServiceName = "api",
            Environment = "prod",
            AggregationLevel = AggregationLevel.OneMinute,
            IntervalStart = DateTimeOffset.UtcNow,
            IntervalEnd = DateTimeOffset.UtcNow
        };

        s1.Id.Should().NotBe(s2.Id);
    }
}
