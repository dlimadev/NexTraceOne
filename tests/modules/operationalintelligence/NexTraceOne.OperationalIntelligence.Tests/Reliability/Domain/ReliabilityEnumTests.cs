using FluentAssertions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;
using Xunit;

namespace NexTraceOne.OperationalIntelligence.Tests.Reliability.Domain;

/// <summary>
/// Testes unitários para os enums do subdomínio Reliability.
/// Verificam definições de valores, combinações de flags e extensibilidade.
/// </summary>
public sealed class ReliabilityEnumTests
{
    // ── ReliabilityStatus ────────────────────────────────────────────

    [Fact]
    public void ReliabilityStatus_ShouldHaveFourValues()
    {
        Enum.GetValues<ReliabilityStatus>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData(ReliabilityStatus.Healthy, 0)]
    [InlineData(ReliabilityStatus.Degraded, 1)]
    [InlineData(ReliabilityStatus.Unavailable, 2)]
    [InlineData(ReliabilityStatus.NeedsAttention, 3)]
    public void ReliabilityStatus_ShouldHaveExpectedValues(ReliabilityStatus status, int expected)
    {
        ((int)status).Should().Be(expected);
    }

    // ── TrendDirection ───────────────────────────────────────────────

    [Fact]
    public void TrendDirection_ShouldHaveThreeValues()
    {
        Enum.GetValues<TrendDirection>().Should().HaveCount(3);
    }

    [Theory]
    [InlineData(TrendDirection.Improving, 0)]
    [InlineData(TrendDirection.Stable, 1)]
    [InlineData(TrendDirection.Declining, 2)]
    public void TrendDirection_ShouldHaveExpectedValues(TrendDirection direction, int expected)
    {
        ((int)direction).Should().Be(expected);
    }

    // ── OperationalFlag ──────────────────────────────────────────────

    [Fact]
    public void OperationalFlag_ShouldHaveSixValues()
    {
        // None + 5 flags
        Enum.GetValues<OperationalFlag>().Should().HaveCount(6);
    }

    [Theory]
    [InlineData(OperationalFlag.None, 0)]
    [InlineData(OperationalFlag.RecentChangeImpact, 1)]
    [InlineData(OperationalFlag.IncidentLinked, 2)]
    [InlineData(OperationalFlag.AnomalyDetected, 4)]
    [InlineData(OperationalFlag.DependencyRisk, 8)]
    [InlineData(OperationalFlag.CoverageGap, 16)]
    public void OperationalFlag_ShouldHaveExpectedValues(OperationalFlag flag, int expected)
    {
        ((int)flag).Should().Be(expected);
    }

    [Fact]
    public void OperationalFlag_ShouldSupportCombinations()
    {
        var combined = OperationalFlag.RecentChangeImpact | OperationalFlag.AnomalyDetected;

        combined.HasFlag(OperationalFlag.RecentChangeImpact).Should().BeTrue();
        combined.HasFlag(OperationalFlag.AnomalyDetected).Should().BeTrue();
        combined.HasFlag(OperationalFlag.IncidentLinked).Should().BeFalse();
    }

    [Fact]
    public void OperationalFlag_None_ShouldNotContainAnyFlag()
    {
        var none = OperationalFlag.None;

        none.HasFlag(OperationalFlag.RecentChangeImpact).Should().BeFalse();
        none.HasFlag(OperationalFlag.IncidentLinked).Should().BeFalse();
        none.HasFlag(OperationalFlag.AnomalyDetected).Should().BeFalse();
        none.HasFlag(OperationalFlag.DependencyRisk).Should().BeFalse();
        none.HasFlag(OperationalFlag.CoverageGap).Should().BeFalse();
    }

    [Fact]
    public void OperationalFlag_AllCombined_ShouldContainAllFlags()
    {
        var all = OperationalFlag.RecentChangeImpact
                | OperationalFlag.IncidentLinked
                | OperationalFlag.AnomalyDetected
                | OperationalFlag.DependencyRisk
                | OperationalFlag.CoverageGap;

        all.HasFlag(OperationalFlag.RecentChangeImpact).Should().BeTrue();
        all.HasFlag(OperationalFlag.IncidentLinked).Should().BeTrue();
        all.HasFlag(OperationalFlag.AnomalyDetected).Should().BeTrue();
        all.HasFlag(OperationalFlag.DependencyRisk).Should().BeTrue();
        all.HasFlag(OperationalFlag.CoverageGap).Should().BeTrue();
    }
}
