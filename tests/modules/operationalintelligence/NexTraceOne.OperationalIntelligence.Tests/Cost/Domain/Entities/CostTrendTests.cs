using NexTraceOne.CostIntelligence.Domain.Entities;
using NexTraceOne.CostIntelligence.Domain.Enums;

namespace NexTraceOne.CostIntelligence.Tests.Domain.Entities;

/// <summary>Testes unitários da entidade CostTrend — classificação automática e invariantes.</summary>
public sealed class CostTrendTests
{
    private static readonly DateTimeOffset FixedStart = new(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FixedEnd = new(2025, 6, 30, 0, 0, 0, TimeSpan.Zero);

    // ── Create com classificação automática ───────────────────────────────

    [Fact]
    public void Create_WithRisingTrend_ShouldClassifyAsRising()
    {
        var result = CostTrend.Create("Svc", "prod", FixedStart, FixedEnd, 100m, 150m, 15m, 30);

        result.IsSuccess.Should().BeTrue();
        result.Value.TrendDirection.Should().Be(TrendDirection.Rising);
        result.Value.IsRising.Should().BeTrue();
        result.Value.IsSignificant.Should().BeTrue();
    }

    [Fact]
    public void Create_WithDecliningTrend_ShouldClassifyAsDeclining()
    {
        var result = CostTrend.Create("Svc", "prod", FixedStart, FixedEnd, 100m, 150m, -10m, 30);

        result.IsSuccess.Should().BeTrue();
        result.Value.TrendDirection.Should().Be(TrendDirection.Declining);
        result.Value.IsDeclining.Should().BeTrue();
        result.Value.IsSignificant.Should().BeTrue();
    }

    [Fact]
    public void Create_WithStableTrend_ShouldClassifyAsStable()
    {
        var result = CostTrend.Create("Svc", "prod", FixedStart, FixedEnd, 100m, 105m, 3m, 30);

        result.IsSuccess.Should().BeTrue();
        result.Value.TrendDirection.Should().Be(TrendDirection.Stable);
        result.Value.IsSignificant.Should().BeFalse();
    }

    [Fact]
    public void Create_WithExactThreshold_ShouldClassifyAsStable()
    {
        var result = CostTrend.Create("Svc", "prod", FixedStart, FixedEnd, 100m, 110m, 5m, 30);

        result.IsSuccess.Should().BeTrue();
        result.Value.TrendDirection.Should().Be(TrendDirection.Stable);
    }

    [Fact]
    public void Create_WithInvalidPeriod_ShouldFail()
    {
        var result = CostTrend.Create("Svc", "prod", FixedEnd, FixedStart, 100m, 150m, 10m, 30);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Period.Invalid");
    }
}
