using FluentAssertions;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GenerateIncidentNarrative;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentDetail;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

using NSubstitute;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Application;

/// <summary>
/// Testes unitários para a feature GenerateIncidentNarrative.
/// Verificam: sucesso, incidente não encontrado, narrativa duplicada.
/// </summary>
public sealed class GenerateIncidentNarrativeTests
{
    private static readonly Guid KnownIncidentGuid = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001");
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly IIncidentStore _store = Substitute.For<IIncidentStore>();
    private readonly IIncidentNarrativeRepository _narrativeRepo = Substitute.For<IIncidentNarrativeRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();

    public GenerateIncidentNarrativeTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _currentTenant.IsActive.Returns(true);
        _currentTenant.Id.Returns(Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_IncidentExists_NoExistingNarrative_ShouldSucceed()
    {
        _store.IncidentExists(KnownIncidentGuid.ToString()).Returns(true);
        _narrativeRepo.GetByIncidentIdAsync(KnownIncidentGuid, Arg.Any<CancellationToken>())
            .Returns((IncidentNarrative?)null);

        var detail = CreateMockDetail();
        _store.GetIncidentDetail(KnownIncidentGuid.ToString()).Returns(detail);

        var handler = new GenerateIncidentNarrative.Handler(_store, _narrativeRepo, _clock, _currentTenant);
        var command = new GenerateIncidentNarrative.Command(KnownIncidentGuid, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NarrativeText.Should().Contain("Incident Narrative");
        result.Value.ModelUsed.Should().Be("template-v1");
        result.Value.GeneratedAt.Should().Be(FixedNow);

        await _narrativeRepo.Received(1).AddAsync(Arg.Any<IncidentNarrative>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IncidentExists_WithModelPreference_ShouldUsePreferredModel()
    {
        _store.IncidentExists(KnownIncidentGuid.ToString()).Returns(true);
        _narrativeRepo.GetByIncidentIdAsync(KnownIncidentGuid, Arg.Any<CancellationToken>())
            .Returns((IncidentNarrative?)null);

        var detail = CreateMockDetail();
        _store.GetIncidentDetail(KnownIncidentGuid.ToString()).Returns(detail);

        var handler = new GenerateIncidentNarrative.Handler(_store, _narrativeRepo, _clock, _currentTenant);
        var command = new GenerateIncidentNarrative.Command(KnownIncidentGuid, "gpt-4o");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ModelUsed.Should().Be("gpt-4o");
    }

    [Fact]
    public async Task Handle_IncidentNotFound_ShouldReturnError()
    {
        _store.IncidentExists(Arg.Any<string>()).Returns(false);

        var handler = new GenerateIncidentNarrative.Handler(_store, _narrativeRepo, _clock, _currentTenant);
        var command = new GenerateIncidentNarrative.Command(Guid.NewGuid(), null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_NarrativeAlreadyExists_ShouldReturnError()
    {
        _store.IncidentExists(KnownIncidentGuid.ToString()).Returns(true);

        var existingNarrative = IncidentNarrative.Create(
            IncidentNarrativeId.New(),
            KnownIncidentGuid,
            "Existing narrative",
            null, null, null, null, null, null,
            "template-v1", 0, NarrativeStatus.Draft, null, FixedNow);

        _narrativeRepo.GetByIncidentIdAsync(KnownIncidentGuid, Arg.Any<CancellationToken>())
            .Returns(existingNarrative);

        var handler = new GenerateIncidentNarrative.Handler(_store, _narrativeRepo, _clock, _currentTenant);
        var command = new GenerateIncidentNarrative.Command(KnownIncidentGuid, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyExists");
    }

    [Fact]
    public void Validator_ShouldRejectEmptyIncidentId()
    {
        var validator = new GenerateIncidentNarrative.Validator();
        var command = new GenerateIncidentNarrative.Command(Guid.Empty, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldAcceptValidCommand()
    {
        var validator = new GenerateIncidentNarrative.Validator();
        var command = new GenerateIncidentNarrative.Command(Guid.NewGuid(), "gpt-4o");

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
                IncidentStatus.Open,
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
                new GetIncidentDetail.TimelineEntry(FixedNow.AddHours(-1), "Incident detected")
            },
            Correlation: new GetIncidentDetail.CorrelationSummary(
                CorrelationConfidence.High,
                "Deploy correlated",
                new[] { new GetIncidentDetail.RelatedChangeItem(Guid.NewGuid(), "Deploy v2.14.0", "Deployment", "High", FixedNow.AddHours(-2)) },
                new[] { new GetIncidentDetail.RelatedServiceItem("svc-payment-gateway", "Payment Gateway", "Primary service affected") }),
            Evidence: new GetIncidentDetail.EvidenceSummary(
                "Elevated error rate",
                "Payment failures increasing",
                new[] { new GetIncidentDetail.EvidenceItem("Error Spike", "500 errors up 300%") }),
            RelatedContracts: Array.Empty<GetIncidentDetail.RelatedContractItem>(),
            Runbooks: Array.Empty<GetIncidentDetail.RunbookItem>(),
            Mitigation: new GetIncidentDetail.MitigationSummary(
                MitigationStatus.InProgress,
                new[] { new GetIncidentDetail.MitigationActionItem("Rollback to v2.13.2", "InProgress", false) },
                "Consider immediate rollback",
                true,
                null));
}
