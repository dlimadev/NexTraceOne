using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Domain.Entities;

/// <summary>Testes unitários da entidade ObservabilityProfile — score, capacidades e atualização.</summary>
public sealed class ObservabilityProfileTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    // ── Assess (score encapsulado) ────────────────────────────────────────

    [Fact]
    public void Assess_WithAllCapabilities_ShouldScoreOne()
    {
        var profile = ObservabilityProfile.Assess("Svc", "prod", true, true, true, true, true, FixedNow);

        profile.ObservabilityScore.Should().Be(1m);
        profile.CapabilityCount.Should().Be(5);
        profile.HasAdequateObservability.Should().BeTrue();
    }

    [Fact]
    public void Assess_WithNoCapabilities_ShouldScoreZero()
    {
        var profile = ObservabilityProfile.Assess("Svc", "dev", false, false, false, false, false, FixedNow);

        profile.ObservabilityScore.Should().Be(0m);
        profile.CapabilityCount.Should().Be(0);
        profile.HasAdequateObservability.Should().BeFalse();
    }

    [Fact]
    public void Assess_WithTracingAndMetrics_ShouldScore050()
    {
        var profile = ObservabilityProfile.Assess("Svc", "prod", true, true, false, false, false, FixedNow);

        profile.ObservabilityScore.Should().Be(0.5m);
        profile.CapabilityCount.Should().Be(2);
        profile.HasAdequateObservability.Should().BeFalse();
    }

    [Fact]
    public void Assess_WithMinimumAdequate_ShouldBeAdequate()
    {
        var profile = ObservabilityProfile.Assess("Svc", "prod", true, true, true, false, false, FixedNow);

        profile.ObservabilityScore.Should().Be(0.7m);
        profile.HasAdequateObservability.Should().BeTrue();
    }

    // ── UpdateCapabilities ────────────────────────────────────────────────

    [Fact]
    public void UpdateCapabilities_ShouldRecalculateScore()
    {
        var profile = ObservabilityProfile.Assess("Svc", "prod", true, false, false, false, false, FixedNow);
        profile.ObservabilityScore.Should().Be(0.25m);

        var later = FixedNow.AddDays(7);
        profile.UpdateCapabilities(true, true, true, true, false, later);

        profile.ObservabilityScore.Should().Be(0.85m);
        profile.HasMetrics.Should().BeTrue();
        profile.HasLogging.Should().BeTrue();
        profile.HasAlerting.Should().BeTrue();
        profile.HasDashboard.Should().BeFalse();
        profile.LastAssessedAt.Should().Be(later);
    }

    [Fact]
    public void UpdateCapabilities_CanDowngrade()
    {
        var profile = ObservabilityProfile.Assess("Svc", "prod", true, true, true, true, true, FixedNow);
        profile.ObservabilityScore.Should().Be(1m);

        profile.UpdateCapabilities(true, false, false, false, false, FixedNow.AddDays(1));

        profile.ObservabilityScore.Should().Be(0.25m);
        profile.CapabilityCount.Should().Be(1);
    }
}
