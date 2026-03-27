using FluentAssertions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;
using NexTraceOne.OperationalIntelligence.Infrastructure.Reliability;
using Xunit;

namespace NexTraceOne.OperationalIntelligence.Tests.Reliability.Application.Calculation;

/// <summary>
/// Testes unitários para ErrorBudgetCalculator (P6.2).
/// Verificam as fórmulas de cálculo de error budget, burn rate e tolerated error rate.
/// </summary>
public sealed class ErrorBudgetCalculatorTests
{
    private static readonly ErrorBudgetCalculator Calculator = new();
    private static readonly Guid TenantId = Guid.NewGuid();

    private static SloDefinition MakeSlo(decimal targetPercent, int windowDays, SloType type = SloType.Availability)
        => SloDefinition.Create(TenantId, "svc-api", "production", "Test SLO", type, targetPercent, windowDays);

    // ── ComputeTotalBudgetMinutes ────────────────────────────────────────────

    [Theory]
    [InlineData(99.9,  30,  43.2)]   // (0.001) × 30 × 1440 = 43.2
    [InlineData(99.0,  30, 432.0)]   // (0.01)  × 30 × 1440 = 432.0
    [InlineData(99.5,   7, 50.4)]    // (0.005) × 7  × 1440 = 50.4
    [InlineData(100.0, 30,   0.0)]   // (0.0)   × 30 × 1440 = 0
    public void ComputeTotalBudgetMinutes_ShouldReturnCorrectValue(
        decimal targetPercent, int windowDays, decimal expected)
    {
        var slo = MakeSlo(targetPercent, windowDays);
        var result = Calculator.ComputeTotalBudgetMinutes(slo);
        result.Should().BeApproximately(expected, 0.001m);
    }

    // ── ComputeConsumedBudgetMinutes ─────────────────────────────────────────

    [Theory]
    [InlineData(0.02, 30, 864.0)]   // 0.02 × 30 × 1440 = 864
    [InlineData(0.001, 30, 43.2)]   // 0.001 × 30 × 1440 = 43.2
    [InlineData(0.0,  30,   0.0)]   // 0% error = 0 consumed
    [InlineData(1.0,   7, 10080.0)] // 100% error × 7 × 1440 = 10080
    public void ComputeConsumedBudgetMinutes_ShouldReturnCorrectValue(
        decimal observedErrorRate, int windowDays, decimal expected)
    {
        var slo = MakeSlo(99.9m, windowDays);
        var result = Calculator.ComputeConsumedBudgetMinutes(slo, observedErrorRate);
        result.Should().BeApproximately(expected, 0.001m);
    }

    [Fact]
    public void ComputeConsumedBudgetMinutes_WithRateAbove1_ShouldClampTo1()
    {
        var slo = MakeSlo(99.9m, 1);
        // 2.0 should be clamped to 1.0 → 1 × 1440 = 1440
        var result = Calculator.ComputeConsumedBudgetMinutes(slo, 2.0m);
        result.Should().Be(1440m);
    }

    // ── ComputeToleratedErrorRate ────────────────────────────────────────────

    [Theory]
    [InlineData(99.9,  0.001)]
    [InlineData(99.0,  0.01)]
    [InlineData(99.5,  0.005)]
    [InlineData(100.0, 0.0)]
    [InlineData(95.0,  0.05)]
    public void ComputeToleratedErrorRate_ShouldReturnComplement(
        decimal targetPercent, decimal expected)
    {
        var slo = MakeSlo(targetPercent, 30);
        var result = Calculator.ComputeToleratedErrorRate(slo);
        result.Should().BeApproximately(expected, 0.00000001m);
    }

    // ── ComputeBurnRate ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(99.9, 0.001, 1.0)]     // observed == tolerated → burn rate 1.0 (sustainable)
    [InlineData(99.9, 0.01,  10.0)]    // 10× faster consumption
    [InlineData(99.9, 0.0001, 0.1)]    // below baseline → burn rate 0.1
    [InlineData(99.9, 0.0,   0.0)]     // no errors → burn rate 0
    public void ComputeBurnRate_ShouldReturnCorrectMultiplier(
        decimal targetPercent, decimal observedErrorRate, decimal expectedBurnRate)
    {
        var slo = MakeSlo(targetPercent, 30);
        var result = Calculator.ComputeBurnRate(slo, observedErrorRate);
        result.Should().BeApproximately(expectedBurnRate, 0.001m);
    }

    [Fact]
    public void ComputeBurnRate_WithZeroToleratedRate_ShouldReturn999WhenObservedPositive()
    {
        // SLO target 100% → tolerated rate = 0 → any observed error → 999 sentinel
        var slo = MakeSlo(100m, 30);
        var result = Calculator.ComputeBurnRate(slo, 0.01m);
        result.Should().Be(999m);
    }

    [Fact]
    public void ComputeBurnRate_WithZeroToleratedAndZeroObserved_ShouldReturn0()
    {
        var slo = MakeSlo(100m, 30);
        var result = Calculator.ComputeBurnRate(slo, 0m);
        result.Should().Be(0m);
    }

    // ── GetWindowHours ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(BurnRateWindow.OneHour,         1)]
    [InlineData(BurnRateWindow.SixHours,        6)]
    [InlineData(BurnRateWindow.TwentyFourHours, 24)]
    [InlineData(BurnRateWindow.SevenDays,       168)]
    public void GetWindowHours_ShouldReturnCorrectHours(BurnRateWindow window, int expectedHours)
    {
        var result = Calculator.GetWindowHours(window);
        result.Should().Be(expectedHours);
    }

    // ── Integration: total vs consumed comparison ────────────────────────────

    [Fact]
    public void WhenObservedRateEqualsToleratedRate_ConsumedShouldEqualTotal()
    {
        var slo = MakeSlo(99.9m, 30);
        var toleratedRate = Calculator.ComputeToleratedErrorRate(slo);
        var total    = Calculator.ComputeTotalBudgetMinutes(slo);
        var consumed = Calculator.ComputeConsumedBudgetMinutes(slo, toleratedRate);
        consumed.Should().BeApproximately(total, 0.01m);
    }

    [Fact]
    public void WhenBurnRateIs2_ConsumedShouldBeDoubleTotalBudget()
    {
        var slo = MakeSlo(99.9m, 30);
        var toleratedRate = Calculator.ComputeToleratedErrorRate(slo);
        var observedRate  = toleratedRate * 2m;
        var total    = Calculator.ComputeTotalBudgetMinutes(slo);
        var consumed = Calculator.ComputeConsumedBudgetMinutes(slo, observedRate);
        var burnRate = Calculator.ComputeBurnRate(slo, observedRate);

        burnRate.Should().BeApproximately(2m, 0.001m);
        consumed.Should().BeApproximately(total * 2m, 0.01m);
    }
}
