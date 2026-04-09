using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Domain.Entities;

/// <summary>Testes unitários da entidade EnvironmentDriftReport — ciclo de vida, invariantes e severidade.</summary>
public sealed class EnvironmentDriftReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 2, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    // ── Generate (valid) ──────────────────────────────────────────────────

    [Fact]
    public void Generate_WithValidInputs_ShouldCreateReport()
    {
        var report = EnvironmentDriftReport.Generate(
            "production", "staging", "ServiceVersions,Configurations",
            "{}", "{}", null, null, null, null,
            3, 1, DriftSeverity.Critical, TenantId, FixedNow);

        report.SourceEnvironment.Should().Be("production");
        report.TargetEnvironment.Should().Be("staging");
        report.AnalyzedDimensions.Should().Be("ServiceVersions,Configurations");
        report.TotalDriftItems.Should().Be(3);
        report.CriticalDriftItems.Should().Be(1);
        report.OverallSeverity.Should().Be(DriftSeverity.Critical);
        report.Status.Should().Be(DriftReportStatus.Generated);
        report.GeneratedAt.Should().Be(FixedNow);
        report.TenantId.Should().Be(TenantId);
        report.Id.Value.Should().NotBe(Guid.Empty);
    }

    // ── Generate (same source/target) ─────────────────────────────────────

    [Fact]
    public void Generate_WithSameSourceAndTarget_ShouldThrow()
    {
        var act = () => EnvironmentDriftReport.Generate(
            "production", "production", "General",
            null, null, null, null, null, null,
            0, 0, DriftSeverity.Low, TenantId, FixedNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Source and target environments must be different*");
    }

    [Fact]
    public void Generate_WithSameSourceAndTargetCaseInsensitive_ShouldThrow()
    {
        var act = () => EnvironmentDriftReport.Generate(
            "Production", "PRODUCTION", "General",
            null, null, null, null, null, null,
            0, 0, DriftSeverity.Low, TenantId, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── Generate (negative total) ─────────────────────────────────────────

    [Fact]
    public void Generate_WithNegativeTotal_ShouldThrow()
    {
        var act = () => EnvironmentDriftReport.Generate(
            "production", "staging", "General",
            null, null, null, null, null, null,
            -1, 0, DriftSeverity.Low, TenantId, FixedNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Total drift items cannot be negative*");
    }

    // ── Generate (critical > total) ───────────────────────────────────────

    [Fact]
    public void Generate_WithCriticalGreaterThanTotal_ShouldThrow()
    {
        var act = () => EnvironmentDriftReport.Generate(
            "production", "staging", "General",
            null, null, null, null, null, null,
            2, 5, DriftSeverity.Low, TenantId, FixedNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Critical drift items must be between 0 and total drift items*");
    }

    [Fact]
    public void Generate_WithNegativeCritical_ShouldThrow()
    {
        var act = () => EnvironmentDriftReport.Generate(
            "production", "staging", "General",
            null, null, null, null, null, null,
            5, -1, DriftSeverity.Low, TenantId, FixedNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Critical drift items must be between 0 and total drift items*");
    }

    // ── Review ────────────────────────────────────────────────────────────

    [Fact]
    public void Review_ShouldTransitionToReviewed()
    {
        var report = CreateReport();
        var reviewedAt = FixedNow.AddHours(2);

        var result = report.Review("Looks good, no action needed.", reviewedAt);

        result.IsSuccess.Should().BeTrue();
        report.Status.Should().Be(DriftReportStatus.Reviewed);
        report.ReviewComment.Should().Be("Looks good, no action needed.");
        report.ReviewedAt.Should().Be(reviewedAt);
    }

    [Fact]
    public void Review_AlreadyReviewed_ShouldReturnError()
    {
        var report = CreateReport();
        report.Review("First review", FixedNow.AddHours(1));

        var result = report.Review("Second review", FixedNow.AddHours(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyReviewed");
    }

    // ── MarkAsStale ───────────────────────────────────────────────────────

    [Fact]
    public void MarkAsStale_ShouldTransitionToStale()
    {
        var report = CreateReport();

        var result = report.MarkAsStale();

        result.IsSuccess.Should().BeTrue();
        report.Status.Should().Be(DriftReportStatus.Stale);
    }

    [Fact]
    public void MarkAsStale_AlreadyStale_ShouldReturnError()
    {
        var report = CreateReport();
        report.MarkAsStale();

        var result = report.MarkAsStale();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyStale");
    }

    // ── Severity derivation ───────────────────────────────────────────────

    [Fact]
    public void Generate_WithZeroDrifts_SeverityLow()
    {
        var report = EnvironmentDriftReport.Generate(
            "prod", "staging", "General",
            null, null, null, null, null, null,
            0, 0, DriftSeverity.Low, TenantId, FixedNow);

        report.OverallSeverity.Should().Be(DriftSeverity.Low);
    }

    [Fact]
    public void Generate_WithCriticalSeverity_ShouldPreserve()
    {
        var report = EnvironmentDriftReport.Generate(
            "prod", "staging", "ServiceVersions",
            "{}", null, null, null, null, null,
            10, 3, DriftSeverity.Critical, TenantId, FixedNow);

        report.OverallSeverity.Should().Be(DriftSeverity.Critical);
    }

    // ── Helper ────────────────────────────────────────────────────────────

    private static EnvironmentDriftReport CreateReport()
        => EnvironmentDriftReport.Generate(
            "production", "staging", "ServiceVersions,Configurations",
            "{}", "{}", null, null, null, null,
            3, 1, DriftSeverity.Medium, TenantId, FixedNow);
}
