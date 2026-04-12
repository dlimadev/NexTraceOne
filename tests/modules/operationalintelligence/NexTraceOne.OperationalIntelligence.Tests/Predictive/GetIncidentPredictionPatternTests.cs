using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetIncidentPredictionPattern;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Predictive;

/// <summary>
/// Testes unitários para o handler e validator de GetIncidentPredictionPattern
/// (Ideia 12 — Predictive Incident Prevention).
/// </summary>
public sealed class GetIncidentPredictionPatternTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 10, 14, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    // ── Handler tests ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExistingPattern_ShouldReturnDetails()
    {
        var repo = Substitute.For<IIncidentPredictionPatternRepository>();
        var pattern = IncidentPredictionPattern.Detect(
            "Friday deploy pattern",
            "Deploys on Friday cause incidents.",
            PredictionPatternType.DeployTiming,
            "svc-order", "OrderService", "production",
            75, 8, 10,
            """{"incidents":["inc-1"]}""",
            """{"dayOfWeek":"Friday"}""",
            """{"action":"Avoid Friday deploys"}""",
            PredictionSeverity.High,
            TenantId,
            FixedNow);

        repo.GetByIdAsync(pattern.Id, Arg.Any<CancellationToken>()).Returns(pattern);

        var handler = new GetIncidentPredictionPattern.Handler(repo);
        var query = new GetIncidentPredictionPattern.Query(pattern.Id.Value);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PatternId.Should().Be(pattern.Id.Value);
        result.Value.PatternName.Should().Be("Friday deploy pattern");
        result.Value.Description.Should().Contain("Deploys on Friday");
        result.Value.PatternType.Should().Be("DeployTiming");
        result.Value.ServiceId.Should().Be("svc-order");
        result.Value.ServiceName.Should().Be("OrderService");
        result.Value.Environment.Should().Be("production");
        result.Value.ConfidencePercent.Should().Be(75);
        result.Value.OccurrenceCount.Should().Be(8);
        result.Value.SampleSize.Should().Be(10);
        result.Value.Severity.Should().Be("High");
        result.Value.Status.Should().Be("Detected");
        result.Value.DetectedAt.Should().Be(FixedNow);
        result.Value.ValidatedAt.Should().BeNull();
        result.Value.ValidationComment.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NonExistingPattern_ShouldReturnNotFound()
    {
        var repo = Substitute.For<IIncidentPredictionPatternRepository>();
        var nonExistentId = Guid.NewGuid();
        repo.GetByIdAsync(IncidentPredictionPatternId.From(nonExistentId), Arg.Any<CancellationToken>())
            .Returns((IncidentPredictionPattern?)null);

        var handler = new GetIncidentPredictionPattern.Handler(repo);
        var query = new GetIncidentPredictionPattern.Query(nonExistentId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INCIDENT_PREDICTION_PATTERN_NOT_FOUND");
    }

    // ── Validator tests ──────────────────────────────────────────────────────

    [Fact]
    public void Validator_EmptyId_ShouldFail()
    {
        var validator = new GetIncidentPredictionPattern.Validator();
        var query = new GetIncidentPredictionPattern.Query(Guid.Empty);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PatternId");
    }
}
