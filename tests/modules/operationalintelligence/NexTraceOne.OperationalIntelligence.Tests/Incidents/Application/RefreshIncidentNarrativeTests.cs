using FluentAssertions;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentDetail;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.RefreshIncidentNarrative;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

using NSubstitute;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Application;

/// <summary>
/// Testes unitários para a feature RefreshIncidentNarrative.
/// Verificam: sucesso, incidente não encontrado, narrativa não encontrada.
/// </summary>
public sealed class RefreshIncidentNarrativeTests
{
    private static readonly Guid KnownIncidentGuid = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001");
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly IIncidentStore _store = Substitute.For<IIncidentStore>();
    private readonly IIncidentNarrativeRepository _narrativeRepo = Substitute.For<IIncidentNarrativeRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public RefreshIncidentNarrativeTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    [Fact]
    public async Task Handle_Success_ShouldRefreshNarrativeAndIncrementCount()
    {
        _store.IncidentExists(KnownIncidentGuid.ToString()).Returns(true);

        var existingNarrative = IncidentNarrative.Create(
            IncidentNarrativeId.New(),
            KnownIncidentGuid,
            "Old narrative",
            null, null, null, null, null, null,
            "template-v1", 0, NarrativeStatus.Draft, null, FixedNow.AddHours(-1));

        _narrativeRepo.GetByIncidentIdAsync(KnownIncidentGuid, Arg.Any<CancellationToken>())
            .Returns(existingNarrative);

        var detail = CreateMockDetail();
        _store.GetIncidentDetail(KnownIncidentGuid.ToString()).Returns(detail);

        var handler = new RefreshIncidentNarrative.Handler(_store, _narrativeRepo, _clock);
        var command = new RefreshIncidentNarrative.Command(KnownIncidentGuid);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NarrativeText.Should().Contain("Incident Narrative");
        result.Value.RefreshCount.Should().Be(1);
        result.Value.RefreshedAt.Should().Be(FixedNow);

        await _narrativeRepo.Received(1).UpdateAsync(Arg.Any<IncidentNarrative>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IncidentNotFound_ShouldReturnError()
    {
        _store.IncidentExists(Arg.Any<string>()).Returns(false);

        var handler = new RefreshIncidentNarrative.Handler(_store, _narrativeRepo, _clock);
        var command = new RefreshIncidentNarrative.Command(Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_NarrativeNotFound_ShouldReturnError()
    {
        _store.IncidentExists(KnownIncidentGuid.ToString()).Returns(true);
        _narrativeRepo.GetByIncidentIdAsync(KnownIncidentGuid, Arg.Any<CancellationToken>())
            .Returns((IncidentNarrative?)null);

        var handler = new RefreshIncidentNarrative.Handler(_store, _narrativeRepo, _clock);
        var command = new RefreshIncidentNarrative.Command(KnownIncidentGuid);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void Validator_ShouldRejectEmptyIncidentId()
    {
        var validator = new RefreshIncidentNarrative.Validator();
        var command = new RefreshIncidentNarrative.Command(Guid.Empty);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldAcceptValidCommand()
    {
        var validator = new RefreshIncidentNarrative.Validator();
        var command = new RefreshIncidentNarrative.Command(Guid.NewGuid());

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    private static GetIncidentDetail.Response CreateMockDetail() =>
        new(
            Identity: new GetIncidentDetail.IncidentIdentity(
                KnownIncidentGuid,
                "INC-2026-0042",
                "Payment Gateway Timeout",
                "High latency on payment processing",
                IncidentType.ServiceDegradation,
                IncidentSeverity.Critical,
                IncidentStatus.Mitigating,
                FixedNow.AddHours(-1),
                FixedNow),
            LinkedServices: new[]
            {
                new GetIncidentDetail.LinkedServiceItem("svc-payment-gateway", "Payment Gateway", "API", "Critical")
            },
            OwnerTeam: "payment-squad",
            ImpactedDomain: "payments",
            ImpactedEnvironment: "Production",
            Timeline: new[]
            {
                new GetIncidentDetail.TimelineEntry(FixedNow.AddHours(-1), "Incident detected"),
                new GetIncidentDetail.TimelineEntry(FixedNow, "Mitigation started")
            },
            Correlation: new GetIncidentDetail.CorrelationSummary(
                CorrelationConfidence.High,
                "Deploy correlated",
                Array.Empty<GetIncidentDetail.RelatedChangeItem>(),
                Array.Empty<GetIncidentDetail.RelatedServiceItem>()),
            Evidence: new GetIncidentDetail.EvidenceSummary(
                "Elevated error rate",
                "Payment failures",
                Array.Empty<GetIncidentDetail.EvidenceItem>()),
            RelatedContracts: Array.Empty<GetIncidentDetail.RelatedContractItem>(),
            Runbooks: Array.Empty<GetIncidentDetail.RunbookItem>(),
            Mitigation: new GetIncidentDetail.MitigationSummary(
                MitigationStatus.InProgress,
                Array.Empty<GetIncidentDetail.MitigationActionItem>(),
                null,
                false,
                null));
}
