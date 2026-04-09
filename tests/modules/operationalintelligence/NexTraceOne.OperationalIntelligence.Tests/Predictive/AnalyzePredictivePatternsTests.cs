using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.AnalyzePredictivePatterns;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Predictive;

/// <summary>
/// Testes unitários para o handler e validator de AnalyzePredictivePatterns
/// (Ideia 12 — Predictive Incident Prevention).
/// </summary>
public sealed class AnalyzePredictivePatternsTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 10, 14, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private static (AnalyzePredictivePatterns.Handler handler,
        IIncidentPredictionPatternRepository repo,
        IUnitOfWork uow) CreateHandler()
    {
        var repo = Substitute.For<IIncidentPredictionPatternRepository>();
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(TenantId);
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        var uow = Substitute.For<IUnitOfWork>();

        var handler = new AnalyzePredictivePatterns.Handler(repo, tenant, clock, uow);
        return (handler, repo, uow);
    }

    private static AnalyzePredictivePatterns.Command CreateValidCommand(
        int confidencePercent = 75,
        string patternType = "DeployTiming",
        string? serviceId = "svc-order",
        int occurrenceCount = 8,
        int sampleSize = 10)
        => new(
            "Friday deploy pattern",
            "Deploys on Friday cause incidents.",
            patternType,
            serviceId,
            "OrderService",
            "production",
            confidencePercent,
            occurrenceCount,
            sampleSize,
            """{"incidents":["inc-1"]}""",
            """{"dayOfWeek":"Friday"}""",
            """{"action":"Avoid Friday deploys"}""");

    // ── Handler tests ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreatePatternAndCommit()
    {
        var (handler, repo, uow) = CreateHandler();
        var cmd = CreateValidCommand();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PatternName.Should().Be("Friday deploy pattern");
        result.Value.PatternType.Should().Be("DeployTiming");
        result.Value.Status.Should().Be("Detected");
        result.Value.DetectedAt.Should().Be(FixedNow);
        repo.Received(1).Add(Arg.Any<IncidentPredictionPattern>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPreviousPatternForService_ShouldMarkAsStale()
    {
        var (handler, repo, uow) = CreateHandler();
        var previousPattern = IncidentPredictionPattern.Detect(
            "Old pattern", "Old description",
            PredictionPatternType.DeployTiming,
            "svc-order", "OrderService", "production",
            60, 5, 10,
            """{"old":true}""", """{"cond":true}""", null,
            PredictionSeverity.Medium, TenantId, FixedNow.AddDays(-7));

        repo.GetLatestByServiceAsync("svc-order", "production", Arg.Any<CancellationToken>())
            .Returns(previousPattern);

        var cmd = CreateValidCommand();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        previousPattern.Status.Should().Be(PredictionPatternStatus.Stale);
        repo.Received(1).Update(previousPattern);
    }

    [Fact]
    public async Task Handle_HighConfidence_ShouldSetCriticalSeverity()
    {
        var (handler, _, _) = CreateHandler();
        var cmd = CreateValidCommand(confidencePercent: 85);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Severity.Should().Be("Critical");
    }

    [Fact]
    public async Task Handle_LowConfidence_ShouldSetLowSeverity()
    {
        var (handler, _, _) = CreateHandler();
        var cmd = CreateValidCommand(confidencePercent: 25);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Severity.Should().Be("Low");
    }

    [Fact]
    public async Task Handle_MediumConfidence_ShouldSetMediumSeverity()
    {
        var (handler, _, _) = CreateHandler();
        var cmd = CreateValidCommand(confidencePercent: 45);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Severity.Should().Be("Medium");
    }

    [Fact]
    public async Task Handle_InvalidPatternType_ShouldReturnError()
    {
        var (handler, _, _) = CreateHandler();
        var cmd = CreateValidCommand(patternType: "InvalidType");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_PATTERN_TYPE");
    }

    [Fact]
    public async Task Handle_NoServiceId_ShouldNotMarkPreviousAsStale()
    {
        var (handler, repo, _) = CreateHandler();
        var cmd = CreateValidCommand(serviceId: null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repo.DidNotReceive().GetLatestByServiceAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Validator tests ──────────────────────────────────────────────────────

    [Fact]
    public void Validator_EmptyPatternName_ShouldFail()
    {
        var validator = new AnalyzePredictivePatterns.Validator();
        var cmd = CreateValidCommand() with { PatternName = "" };

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PatternName");
    }

    [Fact]
    public void Validator_InvalidPatternType_ShouldFail()
    {
        var validator = new AnalyzePredictivePatterns.Validator();
        var cmd = CreateValidCommand() with { PatternType = "NotAValidType" };

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PatternType");
    }

    [Fact]
    public void Validator_OccurrenceExceedsSampleSize_ShouldFail()
    {
        var validator = new AnalyzePredictivePatterns.Validator();
        var cmd = CreateValidCommand(occurrenceCount: 15, sampleSize: 10);

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Occurrence count"));
    }
}
