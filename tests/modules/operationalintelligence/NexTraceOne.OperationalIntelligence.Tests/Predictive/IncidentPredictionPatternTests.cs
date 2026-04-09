using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Predictive;

/// <summary>
/// Testes unitários para a entidade IncidentPredictionPattern (Ideia 12 — Predictive Incident Prevention).
/// Verificam criação, validações, transições de estado e regras de domínio.
/// </summary>
public sealed class IncidentPredictionPatternTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 10, 14, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private static IncidentPredictionPattern CreateValidPattern(
        int confidencePercent = 75,
        int occurrenceCount = 8,
        int sampleSize = 10,
        string? serviceId = "svc-order",
        string? serviceName = "OrderService",
        PredictionPatternType patternType = PredictionPatternType.DeployTiming,
        PredictionSeverity severity = PredictionSeverity.High)
        => IncidentPredictionPattern.Detect(
            "Friday deploy pattern",
            "Deploys on Friday cause incidents 80% of the time.",
            patternType,
            serviceId,
            serviceName,
            "production",
            confidencePercent,
            occurrenceCount,
            sampleSize,
            """{"incidents":["inc-1","inc-2"]}""",
            """{"dayOfWeek":"Friday","timeWindow":"14:00-18:00"}""",
            """{"action":"Avoid Friday deploys"}""",
            severity,
            TenantId,
            FixedNow);

    // ── Detect (factory) ────────────────────────────────────────────────────

    [Fact]
    public void Detect_WithValidInputs_ShouldCreatePattern()
    {
        var pattern = CreateValidPattern();

        pattern.Id.Should().NotBeNull();
        pattern.PatternName.Should().Be("Friday deploy pattern");
        pattern.Description.Should().Contain("Deploys on Friday");
        pattern.PatternType.Should().Be(PredictionPatternType.DeployTiming);
        pattern.ServiceId.Should().Be("svc-order");
        pattern.ServiceName.Should().Be("OrderService");
        pattern.Environment.Should().Be("production");
        pattern.ConfidencePercent.Should().Be(75);
        pattern.OccurrenceCount.Should().Be(8);
        pattern.SampleSize.Should().Be(10);
        pattern.Evidence.Should().Contain("inc-1");
        pattern.TriggerConditions.Should().Contain("Friday");
        pattern.PreventionRecommendations.Should().Contain("Avoid");
        pattern.Severity.Should().Be(PredictionSeverity.High);
        pattern.Status.Should().Be(PredictionPatternStatus.Detected);
        pattern.DetectedAt.Should().Be(FixedNow);
        pattern.TenantId.Should().Be(TenantId);
        pattern.ValidatedAt.Should().BeNull();
        pattern.ValidationComment.Should().BeNull();
    }

    [Fact]
    public void Detect_WithNegativeConfidence_ShouldThrow()
    {
        var act = () => CreateValidPattern(confidencePercent: -1);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("confidencePercent");
    }

    [Fact]
    public void Detect_WithConfidenceAbove100_ShouldThrow()
    {
        var act = () => CreateValidPattern(confidencePercent: 101);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("confidencePercent");
    }

    [Fact]
    public void Detect_WithNegativeOccurrenceCount_ShouldThrow()
    {
        var act = () => CreateValidPattern(occurrenceCount: -1);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("occurrenceCount");
    }

    [Fact]
    public void Detect_WithOccurrenceGreaterThanSampleSize_ShouldThrow()
    {
        var act = () => CreateValidPattern(occurrenceCount: 15, sampleSize: 10);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("occurrenceCount");
    }

    [Fact]
    public void Detect_WithEmptyPatternName_ShouldThrow()
    {
        var act = () => IncidentPredictionPattern.Detect(
            "",
            "Description",
            PredictionPatternType.DeployTiming,
            "svc-1", "Service", "production",
            50, 5, 10,
            """{"data":true}""",
            """{"cond":true}""",
            null,
            PredictionSeverity.Medium,
            TenantId,
            FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Detect_WithEmptyEnvironment_ShouldThrow()
    {
        var act = () => IncidentPredictionPattern.Detect(
            "Pattern",
            "Description",
            PredictionPatternType.DeployTiming,
            "svc-1", "Service", "",
            50, 5, 10,
            """{"data":true}""",
            """{"cond":true}""",
            null,
            PredictionSeverity.Medium,
            TenantId,
            FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Detect_WithNullOptionalFields_ShouldSucceed()
    {
        var pattern = CreateValidPattern(serviceId: null, serviceName: null);

        pattern.ServiceId.Should().BeNull();
        pattern.ServiceName.Should().BeNull();
        pattern.Status.Should().Be(PredictionPatternStatus.Detected);
    }

    // ── Confirm ──────────────────────────────────────────────────────────────

    [Fact]
    public void Confirm_ShouldTransitionToConfirmed()
    {
        var pattern = CreateValidPattern();
        var validatedAt = FixedNow.AddHours(1);

        var result = pattern.Confirm("Valid pattern confirmed by analysis.", validatedAt);

        result.IsSuccess.Should().BeTrue();
        pattern.Status.Should().Be(PredictionPatternStatus.Confirmed);
        pattern.ValidationComment.Should().Be("Valid pattern confirmed by analysis.");
        pattern.ValidatedAt.Should().Be(validatedAt);
    }

    [Fact]
    public void Confirm_AlreadyConfirmed_ShouldReturnError()
    {
        var pattern = CreateValidPattern();
        pattern.Confirm("First confirmation.", FixedNow.AddHours(1));

        var result = pattern.Confirm("Second confirmation.", FixedNow.AddHours(2));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PREDICTION_PATTERN_ALREADY_CONFIRMED");
    }

    // ── Dismiss ──────────────────────────────────────────────────────────────

    [Fact]
    public void Dismiss_ShouldTransitionToDismissed()
    {
        var pattern = CreateValidPattern();
        var validatedAt = FixedNow.AddHours(1);

        var result = pattern.Dismiss("False positive, insufficient evidence.", validatedAt);

        result.IsSuccess.Should().BeTrue();
        pattern.Status.Should().Be(PredictionPatternStatus.Dismissed);
        pattern.ValidationComment.Should().Contain("False positive");
        pattern.ValidatedAt.Should().Be(validatedAt);
    }

    [Fact]
    public void Dismiss_AlreadyDismissed_ShouldReturnError()
    {
        var pattern = CreateValidPattern();
        pattern.Dismiss("First dismissal.", FixedNow.AddHours(1));

        var result = pattern.Dismiss("Second dismissal.", FixedNow.AddHours(2));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PREDICTION_PATTERN_ALREADY_DISMISSED");
    }

    // ── MarkAsStale ──────────────────────────────────────────────────────────

    [Fact]
    public void MarkAsStale_ShouldTransitionToStale()
    {
        var pattern = CreateValidPattern();

        var result = pattern.MarkAsStale();

        result.IsSuccess.Should().BeTrue();
        pattern.Status.Should().Be(PredictionPatternStatus.Stale);
    }

    [Fact]
    public void MarkAsStale_AlreadyStale_ShouldReturnError()
    {
        var pattern = CreateValidPattern();
        pattern.MarkAsStale();

        var result = pattern.MarkAsStale();

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PREDICTION_PATTERN_ALREADY_STALE");
    }
}
