using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.ListIncidentPredictionPatterns;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Predictive;

/// <summary>
/// Testes unitários para o handler e validator de ListIncidentPredictionPatterns
/// (Ideia 12 — Predictive Incident Prevention).
/// </summary>
public sealed class ListIncidentPredictionPatternsTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 10, 14, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private static IncidentPredictionPattern CreatePattern(
        string environment = "production",
        PredictionPatternType patternType = PredictionPatternType.DeployTiming,
        string patternName = "Test Pattern")
        => IncidentPredictionPattern.Detect(
            patternName,
            "Test description.",
            patternType,
            "svc-1", "TestService", environment,
            70, 7, 10,
            """{"data":true}""",
            """{"cond":true}""",
            null,
            PredictionSeverity.High,
            TenantId,
            FixedNow);

    // ── Handler tests ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoFilter_ShouldReturnAll()
    {
        var repo = Substitute.For<IIncidentPredictionPatternRepository>();
        var patterns = new List<IncidentPredictionPattern>
        {
            CreatePattern(patternName: "Pattern A"),
            CreatePattern(patternName: "Pattern B", environment: "staging")
        };
        repo.ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(patterns);

        var handler = new ListIncidentPredictionPatterns.Handler(repo);
        var query = new ListIncidentPredictionPatterns.Query();

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithEnvironmentFilter_ShouldReturnFiltered()
    {
        var repo = Substitute.For<IIncidentPredictionPatternRepository>();
        var patterns = new List<IncidentPredictionPattern>
        {
            CreatePattern(environment: "production")
        };
        repo.ListAsync("production", null, null, Arg.Any<CancellationToken>())
            .Returns(patterns);

        var handler = new ListIncidentPredictionPatterns.Handler(repo);
        var query = new ListIncidentPredictionPatterns.Query(Environment: "production");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items[0].Environment.Should().Be("production");
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldReturnFiltered()
    {
        var repo = Substitute.For<IIncidentPredictionPatternRepository>();
        var patterns = new List<IncidentPredictionPattern>
        {
            CreatePattern()
        };
        repo.ListAsync(null, PredictionPatternStatus.Detected, null, Arg.Any<CancellationToken>())
            .Returns(patterns);

        var handler = new ListIncidentPredictionPatterns.Handler(repo);
        var query = new ListIncidentPredictionPatterns.Query(Status: "Detected");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items[0].Status.Should().Be("Detected");
    }

    [Fact]
    public async Task Handle_EmptyResults_ShouldReturnEmptyList()
    {
        var repo = Substitute.For<IIncidentPredictionPatternRepository>();
        repo.ListAsync(Arg.Any<string?>(), Arg.Any<PredictionPatternStatus?>(),
                Arg.Any<PredictionPatternType?>(), Arg.Any<CancellationToken>())
            .Returns(new List<IncidentPredictionPattern>());

        var handler = new ListIncidentPredictionPatterns.Handler(repo);
        var query = new ListIncidentPredictionPatterns.Query(Environment: "nonexistent");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }

    // ── Validator tests ──────────────────────────────────────────────────────

    [Fact]
    public void Validator_InvalidStatus_ShouldFail()
    {
        var validator = new ListIncidentPredictionPatterns.Validator();
        var query = new ListIncidentPredictionPatterns.Query(Status: "InvalidStatus");

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Invalid status"));
    }
}
