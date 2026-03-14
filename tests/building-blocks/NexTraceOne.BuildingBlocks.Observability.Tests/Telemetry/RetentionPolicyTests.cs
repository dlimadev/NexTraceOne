using FluentAssertions;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Configuration;

namespace NexTraceOne.BuildingBlocks.Observability.Tests.Telemetry;

/// <summary>
/// Testes da política de retenção hot/warm/cold.
/// Valida: defaults seguros por tipo de sinal, separação de bruto vs agregado,
/// cálculo de retenção total, auditoria com política separada.
/// </summary>
public sealed class RetentionPolicyTests
{
    [Fact]
    public void RawTraces_ShouldHaveShortRetention()
    {
        var policy = new RetentionPolicyOptions();
        policy.RawTraces.HotDays.Should().BeLessThanOrEqualTo(14);
        policy.RawTraces.TotalRetentionDays.Should().BeLessThanOrEqualTo(90);
    }

    [Fact]
    public void RawLogs_ShouldHaveShortRetention()
    {
        var policy = new RetentionPolicyOptions();
        policy.RawLogs.HotDays.Should().BeLessThanOrEqualTo(14);
        policy.RawLogs.TotalRetentionDays.Should().BeLessThanOrEqualTo(90);
    }

    [Fact]
    public void MinuteAggregates_ShouldHaveShorterRetentionThanHourly()
    {
        var policy = new RetentionPolicyOptions();
        policy.MinuteAggregates.TotalRetentionDays
            .Should().BeLessThan(policy.HourlyAggregates.TotalRetentionDays);
    }

    [Fact]
    public void HourlyAggregates_ShouldHaveLongerRetention()
    {
        var policy = new RetentionPolicyOptions();
        policy.HourlyAggregates.HotDays.Should().BeGreaterThanOrEqualTo(30);
    }

    [Fact]
    public void Snapshots_ShouldHaveMediumRetention()
    {
        var policy = new RetentionPolicyOptions();
        policy.Snapshots.HotDays.Should().BeGreaterThanOrEqualTo(30);
    }

    [Fact]
    public void AuditCompliance_ShouldHaveLongestRetention()
    {
        var policy = new RetentionPolicyOptions();
        policy.AuditCompliance.TotalRetentionDays
            .Should().BeGreaterThan(policy.HourlyAggregates.TotalRetentionDays);
    }

    [Fact]
    public void AuditCompliance_ShouldHaveColdTierForCompliance()
    {
        var policy = new RetentionPolicyOptions();
        policy.AuditCompliance.ColdDays.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RetentionTier_ShouldCalculateTotalCorrectly()
    {
        var tier = new RetentionTier
        {
            HotDays = 7,
            WarmDays = 30,
            ColdDays = 365
        };
        tier.TotalRetentionDays.Should().Be(402);
    }

    [Fact]
    public void RetentionTier_ZeroDays_ShouldMeanTierNotActive()
    {
        var tier = new RetentionTier
        {
            HotDays = 7,
            WarmDays = 0,
            ColdDays = 0
        };
        tier.TotalRetentionDays.Should().Be(7);
    }

    [Fact]
    public void ObservedTopology_ShouldHaveDedicatedRetention()
    {
        var policy = new RetentionPolicyOptions();
        policy.ObservedTopology.HotDays.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RawTraces_ShouldNotUseColdByDefault()
    {
        var policy = new RetentionPolicyOptions();
        policy.RawTraces.ColdDays.Should().Be(0,
            "traces crus não precisam de cold storage por default — compliance usa audit trail separado");
    }
}
