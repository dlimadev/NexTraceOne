using FluentAssertions;

using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetAnomalyNarrative;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

using NSubstitute;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application.Features;

/// <summary>
/// Testes unitários para a feature GetAnomalyNarrative.
/// Verificam: sucesso, narrativa não encontrada, validação.
/// </summary>
public sealed class GetAnomalyNarrativeTests
{
    private static readonly DriftFindingId KnownDriftFindingId = DriftFindingId.New();
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly IAnomalyNarrativeRepository _narrativeRepo = Substitute.For<IAnomalyNarrativeRepository>();

    [Fact]
    public async Task Handle_NarrativeExists_ShouldReturnSuccess()
    {
        var narrative = AnomalyNarrative.Create(
            AnomalyNarrativeId.New(),
            KnownDriftFindingId,
            "## Anomaly Narrative\nFull text",
            "Symptoms section",
            "Baseline comparison section",
            "Probable cause section",
            "Correlated changes section",
            "Recommended actions section",
            "Severity justification section",
            "template-v1",
            150,
            AnomalyNarrativeStatus.Draft,
            Guid.NewGuid(),
            FixedNow);

        _narrativeRepo.GetByDriftFindingIdAsync(KnownDriftFindingId, Arg.Any<CancellationToken>())
            .Returns(narrative);

        var handler = new GetAnomalyNarrative.Handler(_narrativeRepo);
        var query = new GetAnomalyNarrative.Query(KnownDriftFindingId.Value);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DriftFindingId.Should().Be(KnownDriftFindingId.Value);
        result.Value.NarrativeText.Should().Contain("Anomaly Narrative");
        result.Value.SymptomsSection.Should().Be("Symptoms section");
        result.Value.BaselineComparisonSection.Should().Be("Baseline comparison section");
        result.Value.ProbableCauseSection.Should().Be("Probable cause section");
        result.Value.CorrelatedChangesSection.Should().Be("Correlated changes section");
        result.Value.RecommendedActionsSection.Should().Be("Recommended actions section");
        result.Value.SeverityJustificationSection.Should().Be("Severity justification section");
        result.Value.ModelUsed.Should().Be("template-v1");
        result.Value.TokensUsed.Should().Be(150);
        result.Value.Status.Should().Be("Draft");
        result.Value.RefreshCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NarrativeNotFound_ShouldReturnError()
    {
        _narrativeRepo.GetByDriftFindingIdAsync(Arg.Any<DriftFindingId>(), Arg.Any<CancellationToken>())
            .Returns((AnomalyNarrative?)null);

        var handler = new GetAnomalyNarrative.Handler(_narrativeRepo);
        var query = new GetAnomalyNarrative.Query(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void Validator_ShouldRejectEmptyDriftFindingId()
    {
        var validator = new GetAnomalyNarrative.Validator();
        var query = new GetAnomalyNarrative.Query(Guid.Empty);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldAcceptValidQuery()
    {
        var validator = new GetAnomalyNarrative.Validator();
        var query = new GetAnomalyNarrative.Query(Guid.NewGuid());

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }
}
