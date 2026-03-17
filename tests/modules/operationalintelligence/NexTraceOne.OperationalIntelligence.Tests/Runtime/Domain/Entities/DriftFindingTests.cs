using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Domain.Entities;

/// <summary>Testes unitários da entidade DriftFinding — ciclo de vida, severidade e correlação.</summary>
public sealed class DriftFindingTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    // ── Detect (severidade encapsulada) ───────────────────────────────────

    [Fact]
    public void Detect_WithSmallDeviation_ShouldBeLow()
    {
        var finding = DriftFinding.Detect("Svc", "prod", "AvgLatencyMs", 100m, 105m, FixedNow);

        finding.Severity.Should().Be(DriftSeverity.Low);
        finding.DeviationPercent.Should().Be(5m);
        finding.IsOpen.Should().BeTrue();
    }

    [Fact]
    public void Detect_WithMediumDeviation_ShouldBeMedium()
    {
        var finding = DriftFinding.Detect("Svc", "prod", "AvgLatencyMs", 100m, 115m, FixedNow);

        finding.Severity.Should().Be(DriftSeverity.Medium);
    }

    [Fact]
    public void Detect_WithHighDeviation_ShouldBeHigh()
    {
        var finding = DriftFinding.Detect("Svc", "prod", "P99LatencyMs", 200m, 270m, FixedNow);

        finding.Severity.Should().Be(DriftSeverity.High);
        finding.DeviationPercent.Should().Be(35m);
    }

    [Fact]
    public void Detect_WithCriticalDeviation_ShouldBeCritical()
    {
        var finding = DriftFinding.Detect("Svc", "prod", "ErrorRate", 0.01m, 0.02m, FixedNow);

        finding.Severity.Should().Be(DriftSeverity.Critical);
        finding.DeviationPercent.Should().Be(100m);
    }

    [Fact]
    public void Detect_WithZeroExpected_ShouldHandle()
    {
        var finding = DriftFinding.Detect("Svc", "prod", "ErrorRate", 0m, 0m, FixedNow);

        finding.DeviationPercent.Should().Be(0m);
        finding.Severity.Should().Be(DriftSeverity.Low);
    }

    [Fact]
    public void Detect_WithZeroExpectedAndNonZeroActual_ShouldBe100()
    {
        var finding = DriftFinding.Detect("Svc", "prod", "ErrorRate", 0m, 0.05m, FixedNow);

        finding.DeviationPercent.Should().Be(100m);
        finding.Severity.Should().Be(DriftSeverity.Critical);
    }

    // ── Acknowledge ───────────────────────────────────────────────────────

    [Fact]
    public void Acknowledge_FirstTime_ShouldSucceed()
    {
        var finding = DriftFinding.Detect("Svc", "prod", "AvgLatencyMs", 100m, 130m, FixedNow);

        var result = finding.Acknowledge();

        result.IsSuccess.Should().BeTrue();
        finding.IsAcknowledged.Should().BeTrue();
        finding.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void Acknowledge_Twice_ShouldFail()
    {
        var finding = DriftFinding.Detect("Svc", "prod", "AvgLatencyMs", 100m, 130m, FixedNow);
        finding.Acknowledge();

        var result = finding.Acknowledge();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyAcknowledged");
    }

    // ── Resolve ───────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_WithComment_ShouldSucceed()
    {
        var finding = DriftFinding.Detect("Svc", "prod", "AvgLatencyMs", 100m, 130m, FixedNow);
        var resolvedAt = FixedNow.AddHours(2);

        var result = finding.Resolve("Resolved by scaling up instances", resolvedAt);

        result.IsSuccess.Should().BeTrue();
        finding.IsResolved.Should().BeTrue();
        finding.IsAcknowledged.Should().BeTrue();
        finding.ResolutionComment.Should().Be("Resolved by scaling up instances");
        finding.ResolvedAt.Should().Be(resolvedAt);
    }

    [Fact]
    public void Resolve_Twice_ShouldFail()
    {
        var finding = DriftFinding.Detect("Svc", "prod", "AvgLatencyMs", 100m, 130m, FixedNow);
        finding.Resolve("First fix", FixedNow.AddHours(1));

        var result = finding.Resolve("Second fix", FixedNow.AddHours(2));

        result.IsFailure.Should().BeTrue();
    }

    // ── CorrelateWithRelease ──────────────────────────────────────────────

    [Fact]
    public void CorrelateWithRelease_ShouldSetReleaseId()
    {
        var finding = DriftFinding.Detect("Svc", "prod", "AvgLatencyMs", 100m, 130m, FixedNow);
        var releaseId = Guid.NewGuid();

        var result = finding.CorrelateWithRelease(releaseId);

        result.IsSuccess.Should().BeTrue();
        finding.ReleaseId.Should().Be(releaseId);
    }

    [Fact]
    public void CorrelateWithRelease_WhenAlreadyCorrelated_ShouldFail()
    {
        var finding = DriftFinding.Detect("Svc", "prod", "AvgLatencyMs", 100m, 130m, FixedNow, releaseId: Guid.NewGuid());

        var result = finding.CorrelateWithRelease(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
    }
}
