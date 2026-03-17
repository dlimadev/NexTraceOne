using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Tests.Cost.Domain.Entities;

/// <summary>Testes unitários da entidade CostAttribution — validação de período, cálculo e atualização.</summary>
public sealed class CostAttributionTests
{
    private static readonly DateTimeOffset FixedStart = new(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FixedEnd = new(2025, 6, 30, 23, 59, 59, TimeSpan.Zero);

    // ── Attribute ─────────────────────────────────────────────────────────

    [Fact]
    public void Attribute_WithValidData_ShouldCalculateCostPerRequest()
    {
        var result = CostAttribution.Attribute(Guid.NewGuid(), "OrderApi", FixedStart, FixedEnd, 1000m, 10_000, "prod");

        result.IsSuccess.Should().BeTrue();
        result.Value.CostPerRequest.Should().Be(0.1m);
    }

    [Fact]
    public void Attribute_WithZeroRequests_ShouldSetCostPerRequestToZero()
    {
        var result = CostAttribution.Attribute(Guid.NewGuid(), "BackupSvc", FixedStart, FixedEnd, 50m, 0, "prod");

        result.IsSuccess.Should().BeTrue();
        result.Value.CostPerRequest.Should().Be(0m);
    }

    [Fact]
    public void Attribute_WithInvalidPeriod_ShouldFail()
    {
        var result = CostAttribution.Attribute(Guid.NewGuid(), "Svc", FixedEnd, FixedStart, 100m, 1000, "prod");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Period.Invalid");
    }

    [Fact]
    public void Attribute_WithNegativeCost_ShouldFail()
    {
        var result = CostAttribution.Attribute(Guid.NewGuid(), "Svc", FixedStart, FixedEnd, -10m, 1000, "prod");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Cost.Negative");
    }

    // ── UpdateCosts ───────────────────────────────────────────────────────

    [Fact]
    public void UpdateCosts_WithValidValues_ShouldRecalculateCostPerRequest()
    {
        var attribution = CostAttribution.Attribute(Guid.NewGuid(), "Svc", FixedStart, FixedEnd, 100m, 1000, "prod").Value;

        var result = attribution.UpdateCosts(200m, 4000);

        result.IsSuccess.Should().BeTrue();
        attribution.TotalCost.Should().Be(200m);
        attribution.RequestCount.Should().Be(4000);
        attribution.CostPerRequest.Should().Be(0.05m);
    }

    [Fact]
    public void UpdateCosts_WithNegativeCost_ShouldFail()
    {
        var attribution = CostAttribution.Attribute(Guid.NewGuid(), "Svc", FixedStart, FixedEnd, 100m, 1000, "prod").Value;

        var result = attribution.UpdateCosts(-5m, 1000);

        result.IsFailure.Should().BeTrue();
    }
}
