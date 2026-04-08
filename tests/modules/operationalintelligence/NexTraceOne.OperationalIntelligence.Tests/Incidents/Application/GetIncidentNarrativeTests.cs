using FluentAssertions;

using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentNarrative;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

using NSubstitute;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Application;

/// <summary>
/// Testes unitários para a feature GetIncidentNarrative.
/// Verificam: sucesso e narrativa não encontrada.
/// </summary>
public sealed class GetIncidentNarrativeTests
{
    private static readonly Guid KnownIncidentGuid = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001");
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly IIncidentNarrativeRepository _narrativeRepo = Substitute.For<IIncidentNarrativeRepository>();

    [Fact]
    public async Task Handle_NarrativeExists_ShouldReturnSuccess()
    {
        var narrative = IncidentNarrative.Create(
            IncidentNarrativeId.New(),
            KnownIncidentGuid,
            "## Incident Narrative\nFull text",
            "Symptoms section",
            "Timeline section",
            "Probable cause section",
            "Mitigation section",
            "Related changes section",
            "Affected services section",
            "template-v1",
            150,
            NarrativeStatus.Draft,
            Guid.NewGuid(),
            FixedNow);

        _narrativeRepo.GetByIncidentIdAsync(KnownIncidentGuid, Arg.Any<CancellationToken>())
            .Returns(narrative);

        var handler = new GetIncidentNarrative.Handler(_narrativeRepo);
        var query = new GetIncidentNarrative.Query(KnownIncidentGuid);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IncidentId.Should().Be(KnownIncidentGuid);
        result.Value.NarrativeText.Should().Contain("Incident Narrative");
        result.Value.SymptomsSection.Should().Be("Symptoms section");
        result.Value.TimelineSection.Should().Be("Timeline section");
        result.Value.ProbableCauseSection.Should().Be("Probable cause section");
        result.Value.MitigationSection.Should().Be("Mitigation section");
        result.Value.RelatedChangesSection.Should().Be("Related changes section");
        result.Value.AffectedServicesSection.Should().Be("Affected services section");
        result.Value.ModelUsed.Should().Be("template-v1");
        result.Value.TokensUsed.Should().Be(150);
        result.Value.Status.Should().Be("Draft");
        result.Value.RefreshCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NarrativeNotFound_ShouldReturnError()
    {
        _narrativeRepo.GetByIncidentIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IncidentNarrative?)null);

        var handler = new GetIncidentNarrative.Handler(_narrativeRepo);
        var query = new GetIncidentNarrative.Query(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void Validator_ShouldRejectEmptyIncidentId()
    {
        var validator = new GetIncidentNarrative.Validator();
        var query = new GetIncidentNarrative.Query(Guid.Empty);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldAcceptValidQuery()
    {
        var validator = new GetIncidentNarrative.Validator();
        var query = new GetIncidentNarrative.Query(Guid.NewGuid());

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }
}
