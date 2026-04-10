using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Domain.Entities;

/// <summary>
/// Testes unitários da entidade ResilienceReport — ciclo de vida, invariantes e transições de estado.
/// </summary>
public sealed class ResilienceReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly string TenantId = "tenant-1";
    private static readonly Guid ExperimentId = Guid.NewGuid();

    // ── Generate (valid) ──────────────────────────────────────────────────

    [Fact]
    public void Generate_WithValidInputs_ShouldCreateReport()
    {
        var report = CreateReport();

        report.ChaosExperimentId.Should().Be(ExperimentId);
        report.ServiceName.Should().Be("payment-service");
        report.Environment.Should().Be("Production");
        report.ExperimentType.Should().Be("latency-injection");
        report.ResilienceScore.Should().Be(85);
        report.TheoreticalBlastRadius.Should().Be("{\"services\":[\"order-svc\"]}");
        report.ActualBlastRadius.Should().Be("{\"services\":[\"order-svc\",\"billing-svc\"]}");
        report.BlastRadiusDeviation.Should().Be(15.5m);
        report.TelemetryObservations.Should().NotBeNull();
        report.LatencyImpactMs.Should().Be(120.5m);
        report.ErrorRateImpact.Should().Be(2.3m);
        report.RecoveryTimeSeconds.Should().Be(45);
        report.Status.Should().Be(ResilienceReportStatus.Generated);
        report.GeneratedAt.Should().Be(FixedNow);
        report.TenantId.Should().Be(TenantId);
        report.Id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Generate_WithMinimalOptionalFields_ShouldSucceed()
    {
        var report = ResilienceReport.Generate(
            ExperimentId, "svc", "Dev", "pod-kill", 50,
            null, null, null, null, null, null, null,
            null, null, null, TenantId, FixedNow);

        report.ResilienceScore.Should().Be(50);
        report.TheoreticalBlastRadius.Should().BeNull();
        report.ActualBlastRadius.Should().BeNull();
        report.LatencyImpactMs.Should().BeNull();
        report.RecoveryTimeSeconds.Should().BeNull();
    }

    // ── Generate (invalid) ────────────────────────────────────────────────

    [Fact]
    public void Generate_WithEmptyExperimentId_ShouldThrow()
    {
        var act = () => ResilienceReport.Generate(
            Guid.Empty, "svc", "Dev", "pod-kill", 50,
            null, null, null, null, null, null, null,
            null, null, null, TenantId, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_WithEmptyServiceName_ShouldThrow()
    {
        var act = () => ResilienceReport.Generate(
            ExperimentId, "", "Dev", "pod-kill", 50,
            null, null, null, null, null, null, null,
            null, null, null, TenantId, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_WithEmptyEnvironment_ShouldThrow()
    {
        var act = () => ResilienceReport.Generate(
            ExperimentId, "svc", "", "pod-kill", 50,
            null, null, null, null, null, null, null,
            null, null, null, TenantId, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_WithEmptyExperimentType_ShouldThrow()
    {
        var act = () => ResilienceReport.Generate(
            ExperimentId, "svc", "Dev", "", 50,
            null, null, null, null, null, null, null,
            null, null, null, TenantId, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_WithScoreBelowZero_ShouldThrow()
    {
        var act = () => ResilienceReport.Generate(
            ExperimentId, "svc", "Dev", "pod-kill", -1,
            null, null, null, null, null, null, null,
            null, null, null, TenantId, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_WithScoreAbove100_ShouldThrow()
    {
        var act = () => ResilienceReport.Generate(
            ExperimentId, "svc", "Dev", "pod-kill", 101,
            null, null, null, null, null, null, null,
            null, null, null, TenantId, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_WithEmptyTenantId_ShouldThrow()
    {
        var act = () => ResilienceReport.Generate(
            ExperimentId, "svc", "Dev", "pod-kill", 50,
            null, null, null, null, null, null, null,
            null, null, null, "", FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── Review ────────────────────────────────────────────────────────────

    [Fact]
    public void Review_ShouldTransitionToReviewed()
    {
        var report = CreateReport();
        var reviewedAt = FixedNow.AddHours(2);

        var result = report.Review("user-42", "Service handled experiment well.", reviewedAt);

        result.IsSuccess.Should().BeTrue();
        report.Status.Should().Be(ResilienceReportStatus.Reviewed);
        report.ReviewedByUserId.Should().Be("user-42");
        report.ReviewComment.Should().Be("Service handled experiment well.");
        report.ReviewedAt.Should().Be(reviewedAt);
    }

    [Fact]
    public void Review_AlreadyReviewed_ShouldReturnError()
    {
        var report = CreateReport();
        report.Review("user-1", "First review", FixedNow.AddHours(1));

        var result = report.Review("user-2", "Second review", FixedNow.AddHours(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyReviewed");
    }

    [Fact]
    public void Review_WhenArchived_ShouldReturnError()
    {
        var report = CreateReport();
        report.Archive();

        var result = report.Review("user-1", "Review after archive", FixedNow.AddHours(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyArchived");
    }

    // ── Archive ───────────────────────────────────────────────────────────

    [Fact]
    public void Archive_FromGenerated_ShouldTransitionToArchived()
    {
        var report = CreateReport();

        var result = report.Archive();

        result.IsSuccess.Should().BeTrue();
        report.Status.Should().Be(ResilienceReportStatus.Archived);
    }

    [Fact]
    public void Archive_FromReviewed_ShouldTransitionToArchived()
    {
        var report = CreateReport();
        report.Review("user-1", "Looks good", FixedNow.AddHours(1));

        var result = report.Archive();

        result.IsSuccess.Should().BeTrue();
        report.Status.Should().Be(ResilienceReportStatus.Archived);
    }

    [Fact]
    public void Archive_AlreadyArchived_ShouldReturnError()
    {
        var report = CreateReport();
        report.Archive();

        var result = report.Archive();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyArchived");
    }

    // ── Boundary values ─────────────────────────────────────────────────

    [Fact]
    public void Generate_WithScoreZero_ShouldSucceed()
    {
        var report = ResilienceReport.Generate(
            ExperimentId, "svc", "Dev", "pod-kill", 0,
            null, null, null, null, null, null, null,
            null, null, null, TenantId, FixedNow);

        report.ResilienceScore.Should().Be(0);
    }

    [Fact]
    public void Generate_WithScore100_ShouldSucceed()
    {
        var report = ResilienceReport.Generate(
            ExperimentId, "svc", "Dev", "pod-kill", 100,
            null, null, null, null, null, null, null,
            null, null, null, TenantId, FixedNow);

        report.ResilienceScore.Should().Be(100);
    }

    // ── Helper ────────────────────────────────────────────────────────────

    private static ResilienceReport CreateReport()
        => ResilienceReport.Generate(
            ExperimentId,
            "payment-service",
            "Production",
            "latency-injection",
            85,
            "{\"services\":[\"order-svc\"]}",
            "{\"services\":[\"order-svc\",\"billing-svc\"]}",
            15.5m,
            "{\"p99_latency\":250,\"error_count\":12}",
            120.5m,
            2.3m,
            45,
            "[\"Circuit breaker activated correctly\"]",
            "[\"No retry on downstream timeout\"]",
            "[\"Add retry policy to billing client\"]",
            TenantId,
            FixedNow);
}
