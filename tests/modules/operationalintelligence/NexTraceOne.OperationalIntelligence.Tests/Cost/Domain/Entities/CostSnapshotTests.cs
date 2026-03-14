using NexTraceOne.CostIntelligence.Domain.Entities;

namespace NexTraceOne.CostIntelligence.Tests.Domain.Entities;

/// <summary>Testes unitários da entidade CostSnapshot — validação de invariantes e comportamento.</summary>
public sealed class CostSnapshotTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    // ── Create ────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var result = CostSnapshot.Create("OrderService", "prod", 100m, 30m, 25m, 20m, 15m, FixedNow, "CloudWatch", "daily");

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("OrderService");
        result.Value.Environment.Should().Be("prod");
        result.Value.TotalCost.Should().Be(100m);
        result.Value.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_WithSharesSumEqualToTotal_ShouldSucceed()
    {
        var result = CostSnapshot.Create("Svc", "prod", 100m, 25m, 25m, 25m, 25m, FixedNow, "Prom", "hourly");

        result.IsSuccess.Should().BeTrue();
        result.Value.SharesSum.Should().Be(100m);
        result.Value.UnattributedCost.Should().Be(0m);
    }

    [Fact]
    public void Create_WithSharesExceedingTotal_ShouldFail()
    {
        var result = CostSnapshot.Create("Svc", "prod", 100m, 50m, 30m, 20m, 10m, FixedNow, "Prom", "daily");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidCostShares");
    }

    [Fact]
    public void Create_WithSharesBelowTotal_ShouldShowUnattributedCost()
    {
        var result = CostSnapshot.Create("Svc", "prod", 100m, 20m, 15m, 10m, 5m, FixedNow, "Prom", "daily");

        result.IsSuccess.Should().BeTrue();
        result.Value.SharesSum.Should().Be(50m);
        result.Value.UnattributedCost.Should().Be(50m);
    }

    [Fact]
    public void Create_WithZeroCost_ShouldSucceed()
    {
        var result = CostSnapshot.Create("Svc", "dev", 0m, 0m, 0m, 0m, 0m, FixedNow, "Prom", "hourly");

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCost.Should().Be(0m);
    }

    // ── IsAnomaly ─────────────────────────────────────────────────────────

    [Fact]
    public void IsAnomaly_WhenCostExceedsThreshold_ShouldReturnTrue()
    {
        var result = CostSnapshot.Create("Svc", "prod", 150m, 50m, 40m, 30m, 20m, FixedNow, "Prom", "daily");
        var snapshot = result.Value;

        snapshot.IsAnomaly(expectedCost: 100m, thresholdPercent: 20m).Should().BeTrue();
    }

    [Fact]
    public void IsAnomaly_WhenCostWithinThreshold_ShouldReturnFalse()
    {
        var result = CostSnapshot.Create("Svc", "prod", 115m, 40m, 30m, 25m, 15m, FixedNow, "Prom", "daily");
        var snapshot = result.Value;

        snapshot.IsAnomaly(expectedCost: 100m, thresholdPercent: 20m).Should().BeFalse();
    }

    [Fact]
    public void IsAnomaly_WithZeroExpectedCost_ShouldReturnFalse()
    {
        var result = CostSnapshot.Create("Svc", "prod", 50m, 20m, 10m, 10m, 5m, FixedNow, "Prom", "daily");
        var snapshot = result.Value;

        snapshot.IsAnomaly(expectedCost: 0m, thresholdPercent: 20m).Should().BeFalse();
    }

    // ── CalculateDeviationPercent ─────────────────────────────────────────

    [Fact]
    public void CalculateDeviationPercent_WhenCostAboveExpected_ShouldReturnPositive()
    {
        var result = CostSnapshot.Create("Svc", "prod", 120m, 40m, 30m, 25m, 15m, FixedNow, "Prom", "daily");
        var snapshot = result.Value;

        snapshot.CalculateDeviationPercent(100m).Should().Be(20m);
    }

    [Fact]
    public void CalculateDeviationPercent_WhenCostBelowExpected_ShouldReturnNegative()
    {
        var result = CostSnapshot.Create("Svc", "prod", 80m, 30m, 20m, 15m, 10m, FixedNow, "Prom", "daily");
        var snapshot = result.Value;

        snapshot.CalculateDeviationPercent(100m).Should().Be(-20m);
    }

    [Fact]
    public void CalculateDeviationPercent_WhenExpectedIsZero_ShouldReturn100()
    {
        var result = CostSnapshot.Create("Svc", "prod", 50m, 20m, 10m, 10m, 5m, FixedNow, "Prom", "daily");
        var snapshot = result.Value;

        snapshot.CalculateDeviationPercent(0m).Should().Be(100m);
    }

    [Fact]
    public void CalculateDeviationPercent_WhenBothZero_ShouldReturnZero()
    {
        var result = CostSnapshot.Create("Svc", "dev", 0m, 0m, 0m, 0m, 0m, FixedNow, "Prom", "hourly");
        var snapshot = result.Value;

        snapshot.CalculateDeviationPercent(0m).Should().Be(0m);
    }
}
