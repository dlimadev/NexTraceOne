using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Tests.Cost.Domain.Entities;

/// <summary>Testes unitários da entidade ServiceCostProfile — orçamento, alertas e ciclo mensal.</summary>
public sealed class ServiceCostProfileTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    // ── Create ────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithBudget_ShouldInitializeCorrectly()
    {
        var profile = ServiceCostProfile.Create("OrderSvc", "prod", 80m, FixedNow, monthlyBudget: 5000m);

        profile.ServiceName.Should().Be("OrderSvc");
        profile.MonthlyBudget.Should().Be(5000m);
        profile.CurrentMonthCost.Should().Be(0m);
        profile.IsOverBudget.Should().BeFalse();
    }

    [Fact]
    public void Create_WithoutBudget_ShouldHaveNullBudget()
    {
        var profile = ServiceCostProfile.Create("Svc", "dev", 80m, FixedNow);

        profile.MonthlyBudget.Should().BeNull();
        profile.IsOverBudget.Should().BeFalse();
        profile.BudgetUsagePercent.Should().BeNull();
    }

    // ── UpdateCurrentCost ─────────────────────────────────────────────────

    [Fact]
    public void UpdateCurrentCost_ShouldUpdateAndTrackTimestamp()
    {
        var profile = ServiceCostProfile.Create("Svc", "prod", 80m, FixedNow, 1000m);
        var later = FixedNow.AddHours(1);

        var result = profile.UpdateCurrentCost(500m, later);

        result.IsSuccess.Should().BeTrue();
        profile.CurrentMonthCost.Should().Be(500m);
        profile.LastUpdatedAt.Should().Be(later);
    }

    [Fact]
    public void UpdateCurrentCost_WithNegative_ShouldFail()
    {
        var profile = ServiceCostProfile.Create("Svc", "prod", 80m, FixedNow, 1000m);

        var result = profile.UpdateCurrentCost(-10m, FixedNow);

        result.IsFailure.Should().BeTrue();
    }

    // ── CheckBudgetAlert ──────────────────────────────────────────────────

    [Fact]
    public void CheckBudgetAlert_WhenOverThreshold_ShouldReturnError()
    {
        var profile = ServiceCostProfile.Create("Svc", "prod", 80m, FixedNow, 1000m);
        profile.UpdateCurrentCost(850m, FixedNow);

        var result = profile.CheckBudgetAlert();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("BudgetExceeded");
    }

    [Fact]
    public void CheckBudgetAlert_WhenUnderThreshold_ShouldSucceed()
    {
        var profile = ServiceCostProfile.Create("Svc", "prod", 80m, FixedNow, 1000m);
        profile.UpdateCurrentCost(500m, FixedNow);

        var result = profile.CheckBudgetAlert();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CheckBudgetAlert_WithNoBudget_ShouldSucceed()
    {
        var profile = ServiceCostProfile.Create("Svc", "dev", 80m, FixedNow);

        var result = profile.CheckBudgetAlert();

        result.IsSuccess.Should().BeTrue();
    }

    // ── BudgetUsagePercent ────────────────────────────────────────────────

    [Fact]
    public void BudgetUsagePercent_ShouldCalculateCorrectly()
    {
        var profile = ServiceCostProfile.Create("Svc", "prod", 80m, FixedNow, 1000m);
        profile.UpdateCurrentCost(750m, FixedNow);

        profile.BudgetUsagePercent.Should().Be(75m);
    }

    // ── ResetMonthlyCycle ─────────────────────────────────────────────────

    [Fact]
    public void ResetMonthlyCycle_ShouldResetCostToZero()
    {
        var profile = ServiceCostProfile.Create("Svc", "prod", 80m, FixedNow, 1000m);
        profile.UpdateCurrentCost(800m, FixedNow);
        var newMonth = FixedNow.AddMonths(1);

        profile.ResetMonthlyCycle(newMonth);

        profile.CurrentMonthCost.Should().Be(0m);
        profile.LastUpdatedAt.Should().Be(newMonth);
    }

    // ── SetBudget ─────────────────────────────────────────────────────────

    [Fact]
    public void SetBudget_ShouldUpdateBudget()
    {
        var profile = ServiceCostProfile.Create("Svc", "prod", 80m, FixedNow, 1000m);

        var result = profile.SetBudget(2000m, FixedNow);

        result.IsSuccess.Should().BeTrue();
        profile.MonthlyBudget.Should().Be(2000m);
    }

    [Fact]
    public void SetBudget_WithNegative_ShouldFail()
    {
        var profile = ServiceCostProfile.Create("Svc", "prod", 80m, FixedNow, 1000m);

        var result = profile.SetBudget(-100m, FixedNow);

        result.IsFailure.Should().BeTrue();
    }

    // ── UpdateAlertThreshold ──────────────────────────────────────────────

    [Fact]
    public void UpdateAlertThreshold_WithValidValue_ShouldSucceed()
    {
        var profile = ServiceCostProfile.Create("Svc", "prod", 80m, FixedNow, 1000m);

        var result = profile.UpdateAlertThreshold(90m, FixedNow);

        result.IsSuccess.Should().BeTrue();
        profile.AlertThresholdPercent.Should().Be(90m);
    }
}
