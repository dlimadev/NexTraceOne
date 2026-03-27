using FluentAssertions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;
using Xunit;

namespace NexTraceOne.OperationalIntelligence.Tests.Reliability.Domain.Entities;

/// <summary>
/// Testes unitários para as entidades de SLO, SLA, ErrorBudget e BurnRate.
/// Verificam criação, validação e lógica de negócio do domínio de Reliability (P6.1).
/// </summary>
public sealed class SloSlaEntityTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    // ── SloDefinition ────────────────────────────────────────────────────────

    [Fact]
    public void SloDefinition_Create_ShouldReturnValidEntity()
    {
        var slo = SloDefinition.Create(TenantId, "svc-api", "production", "API Availability", SloType.Availability, 99.9m, 30);

        slo.TenantId.Should().Be(TenantId);
        slo.ServiceId.Should().Be("svc-api");
        slo.Environment.Should().Be("production");
        slo.Name.Should().Be("API Availability");
        slo.Type.Should().Be(SloType.Availability);
        slo.TargetPercent.Should().Be(99.9m);
        slo.WindowDays.Should().Be(30);
        slo.IsActive.Should().BeTrue();
        slo.Id.Should().NotBeNull();
    }

    [Fact]
    public void SloDefinition_Create_WithAlertThreshold_ShouldSetThreshold()
    {
        var slo = SloDefinition.Create(TenantId, "svc-api", "production", "Latency SLO", SloType.Latency, 95m, 7, alertThresholdPercent: 90m);

        slo.AlertThresholdPercent.Should().Be(90m);
    }

    [Fact]
    public void SloDefinition_Deactivate_ShouldSetIsActiveFalse()
    {
        var slo = SloDefinition.Create(TenantId, "svc-api", "production", "Test SLO", SloType.ErrorRate, 99m, 30);
        slo.Deactivate();

        slo.IsActive.Should().BeFalse();
    }

    [Fact]
    public void SloDefinition_UpdateTarget_ShouldChangeValues()
    {
        var slo = SloDefinition.Create(TenantId, "svc-api", "production", "Test SLO", SloType.Availability, 99m, 30);
        slo.UpdateTarget(99.9m, 99m);

        slo.TargetPercent.Should().Be(99.9m);
        slo.AlertThresholdPercent.Should().Be(99m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void SloDefinition_Create_WithInvalidTarget_ShouldThrow(decimal target)
    {
        var act = () => SloDefinition.Create(TenantId, "svc-api", "production", "Test SLO", SloType.Availability, target, 30);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void SloDefinition_Create_WithZeroWindowDays_ShouldThrow()
    {
        var act = () => SloDefinition.Create(TenantId, "svc-api", "production", "Test SLO", SloType.Availability, 99m, 0);

        act.Should().Throw<Exception>();
    }

    // ── SlaDefinition ────────────────────────────────────────────────────────

    [Fact]
    public void SlaDefinition_Create_ShouldReturnValidEntity()
    {
        var sloId = SloDefinitionId.New();
        var sla = SlaDefinition.Create(TenantId, sloId, "API SLA Tier-1", 99.5m, DateTimeOffset.UtcNow);

        sla.TenantId.Should().Be(TenantId);
        sla.SloDefinitionId.Should().Be(sloId);
        sla.Name.Should().Be("API SLA Tier-1");
        sla.ContractualTargetPercent.Should().Be(99.5m);
        sla.Status.Should().Be(SlaStatus.Active);
        sla.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SlaDefinition_MarkBreached_ShouldSetStatusBreached()
    {
        var sloId = SloDefinitionId.New();
        var sla = SlaDefinition.Create(TenantId, sloId, "SLA Test", 99m, DateTimeOffset.UtcNow);
        sla.MarkBreached();

        sla.Status.Should().Be(SlaStatus.Breached);
    }

    [Fact]
    public void SlaDefinition_MarkAtRisk_ShouldSetStatusAtRisk()
    {
        var sloId = SloDefinitionId.New();
        var sla = SlaDefinition.Create(TenantId, sloId, "SLA Test", 99m, DateTimeOffset.UtcNow);
        sla.MarkAtRisk();

        sla.Status.Should().Be(SlaStatus.AtRisk);
    }

    [Fact]
    public void SlaDefinition_MarkActive_ShouldRestoreActiveStatus()
    {
        var sloId = SloDefinitionId.New();
        var sla = SlaDefinition.Create(TenantId, sloId, "SLA Test", 99m, DateTimeOffset.UtcNow);
        sla.MarkBreached();
        sla.MarkActive();

        sla.Status.Should().Be(SlaStatus.Active);
    }

    // ── ErrorBudgetSnapshot ──────────────────────────────────────────────────

    [Fact]
    public void ErrorBudgetSnapshot_Create_ShouldComputeRemainingAndPercent()
    {
        var sloId = SloDefinitionId.New();
        var snapshot = ErrorBudgetSnapshot.Create(TenantId, sloId, "svc-api", "production", 1440m, 288m, DateTimeOffset.UtcNow);

        snapshot.TotalBudgetMinutes.Should().Be(1440m);
        snapshot.ConsumedBudgetMinutes.Should().Be(288m);
        snapshot.RemainingBudgetMinutes.Should().Be(1152m);
        snapshot.ConsumedPercent.Should().Be(20m);
        snapshot.Status.Should().Be(SloStatus.Healthy);
    }

    [Fact]
    public void ErrorBudgetSnapshot_Create_WhenConsumedAbove80Pct_ShouldBeAtRisk()
    {
        var sloId = SloDefinitionId.New();
        var snapshot = ErrorBudgetSnapshot.Create(TenantId, sloId, "svc-api", "production", 100m, 90m, DateTimeOffset.UtcNow);

        snapshot.Status.Should().Be(SloStatus.AtRisk);
    }

    [Fact]
    public void ErrorBudgetSnapshot_Create_WhenConsumedFully_ShouldBeViolated()
    {
        var sloId = SloDefinitionId.New();
        var snapshot = ErrorBudgetSnapshot.Create(TenantId, sloId, "svc-api", "production", 100m, 100m, DateTimeOffset.UtcNow);

        snapshot.Status.Should().Be(SloStatus.Violated);
        snapshot.RemainingBudgetMinutes.Should().Be(0m);
        snapshot.ConsumedPercent.Should().Be(100m);
    }

    [Fact]
    public void ErrorBudgetSnapshot_Create_WhenConsumedExceedsTotal_ShouldClampRemaining()
    {
        var sloId = SloDefinitionId.New();
        var snapshot = ErrorBudgetSnapshot.Create(TenantId, sloId, "svc-api", "production", 100m, 150m, DateTimeOffset.UtcNow);

        snapshot.RemainingBudgetMinutes.Should().Be(0m);
        snapshot.Status.Should().Be(SloStatus.Violated);
    }

    // ── BurnRateSnapshot ─────────────────────────────────────────────────────

    [Fact]
    public void BurnRateSnapshot_Create_ShouldComputeBurnRate()
    {
        var sloId = SloDefinitionId.New();
        // Observed = 0.01 (1% errors), tolerated = 0.001 (0.1% errors) => burn rate = 10
        var snapshot = BurnRateSnapshot.Create(TenantId, sloId, "svc-api", "production",
            BurnRateWindow.OneHour, 0.01m, 0.001m, DateTimeOffset.UtcNow);

        snapshot.BurnRate.Should().Be(10m);
        snapshot.Status.Should().Be(SloStatus.AtRisk); // >= 6
    }

    [Fact]
    public void BurnRateSnapshot_Create_WithLowBurnRate_ShouldBeHealthy()
    {
        var sloId = SloDefinitionId.New();
        var snapshot = BurnRateSnapshot.Create(TenantId, sloId, "svc-api", "production",
            BurnRateWindow.SevenDays, 0.0005m, 0.001m, DateTimeOffset.UtcNow);

        snapshot.BurnRate.Should().Be(0.5m);
        snapshot.Status.Should().Be(SloStatus.Healthy);
    }

    [Fact]
    public void BurnRateSnapshot_Create_WithCriticalBurnRate_ShouldBeViolated()
    {
        var sloId = SloDefinitionId.New();
        // Burn rate >= 14.4 => Violated
        var snapshot = BurnRateSnapshot.Create(TenantId, sloId, "svc-api", "production",
            BurnRateWindow.OneHour, 0.015m, 0.001m, DateTimeOffset.UtcNow);

        snapshot.BurnRate.Should().Be(15m);
        snapshot.Status.Should().Be(SloStatus.Violated);
    }

    [Fact]
    public void BurnRateSnapshot_Create_WithZeroToleratedRate_ShouldHandleGracefully()
    {
        var sloId = SloDefinitionId.New();
        var snapshot = BurnRateSnapshot.Create(TenantId, sloId, "svc-api", "production",
            BurnRateWindow.TwentyFourHours, 0m, 0m, DateTimeOffset.UtcNow);

        snapshot.BurnRate.Should().Be(0m);
        snapshot.Status.Should().Be(SloStatus.Healthy);
    }
}
