using FluentAssertions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentCorrelation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentDetail;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentEvidence;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentMitigation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentSummary;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidents;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidentsByService;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidentsByTeam;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using Xunit;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Application;

/// <summary>
/// Testes unitários para as features do subdomínio Incidents.
/// Verificam handlers, validators e respostas de todas as queries.
/// </summary>
public sealed class IncidentFeatureTests
{
    // ── ListIncidents ────────────────────────────────────────────────

    [Fact]
    public async Task ListIncidents_WithNoFilters_ShouldReturnAllItems()
    {
        var handler = new ListIncidents.Handler();
        var query = new ListIncidents.Query(null, null, null, null, null, null, null, null, null, 1, 100);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().NotBeEmpty();
        result.Value.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ListIncidents_FilterBySeverity_ShouldReturnFiltered()
    {
        var handler = new ListIncidents.Handler();
        var query = new ListIncidents.Query(null, null, null, IncidentSeverity.Critical, null, null, null, null, null, 1, 100);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().AllSatisfy(i => i.Severity.Should().Be(IncidentSeverity.Critical));
    }

    [Fact]
    public async Task ListIncidents_FilterByStatus_ShouldReturnFiltered()
    {
        var handler = new ListIncidents.Handler();
        var query = new ListIncidents.Query(null, null, null, null, IncidentStatus.Mitigating, null, null, null, null, 1, 100);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().AllSatisfy(i => i.Status.Should().Be(IncidentStatus.Mitigating));
    }

    [Fact]
    public async Task ListIncidents_FilterByTeam_ShouldReturnFiltered()
    {
        var handler = new ListIncidents.Handler();
        var query = new ListIncidents.Query("order-squad", null, null, null, null, null, null, null, null, 1, 100);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().AllSatisfy(i => i.OwnerTeam.Should().Be("order-squad"));
    }

    [Fact]
    public async Task ListIncidents_FilterBySearch_ShouldReturnMatching()
    {
        var handler = new ListIncidents.Handler();
        var query = new ListIncidents.Query(null, null, null, null, null, null, "payment", null, null, 1, 100);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
    }

    [Fact]
    public void ListIncidents_Validator_ShouldRejectInvalidPage()
    {
        var validator = new ListIncidents.Validator();
        var query = new ListIncidents.Query(null, null, null, null, null, null, null, null, null, 0, 10);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListIncidents_Validator_ShouldAcceptValidQuery()
    {
        var validator = new ListIncidents.Validator();
        var query = new ListIncidents.Query("team-a", null, null, null, null, null, null, null, null, 1, 20);

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    // ── GetIncidentDetail ────────────────────────────────────────────

    [Fact]
    public async Task GetIncidentDetail_KnownIncident_ShouldReturnDetail()
    {
        var handler = new GetIncidentDetail.Handler();
        var query = new GetIncidentDetail.Query("a1b2c3d4-0001-0000-0000-000000000001");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Identity.Reference.Should().Be("INC-2026-0042");
        result.Value.OwnerTeam.Should().Be("payment-squad");
        result.Value.Identity.Severity.Should().Be(IncidentSeverity.Critical);
        result.Value.LinkedServices.Should().NotBeEmpty();
        result.Value.Timeline.Should().NotBeEmpty();
        result.Value.Correlation.Confidence.Should().Be(CorrelationConfidence.High);
        result.Value.Runbooks.Should().NotBeEmpty();
        result.Value.Mitigation.Status.Should().Be(MitigationStatus.InProgress);
    }

    [Fact]
    public async Task GetIncidentDetail_UnknownIncident_ShouldReturnNotFound()
    {
        var handler = new GetIncidentDetail.Handler();
        var query = new GetIncidentDetail.Query("nonexistent-incident-id");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void GetIncidentDetail_Validator_ShouldRejectEmptyId()
    {
        var validator = new GetIncidentDetail.Validator();
        var query = new GetIncidentDetail.Query("");

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    // ── GetIncidentCorrelation ───────────────────────────────────────

    [Fact]
    public async Task GetIncidentCorrelation_KnownIncident_ShouldReturnCorrelation()
    {
        var handler = new GetIncidentCorrelation.Handler();
        var query = new GetIncidentCorrelation.Query("a1b2c3d4-0001-0000-0000-000000000001");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Confidence.Should().Be(CorrelationConfidence.High);
        result.Value.RelatedChanges.Should().NotBeEmpty();
        result.Value.RelatedServices.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetIncidentCorrelation_LowConfidenceIncident_ShouldReturnLowConfidence()
    {
        var handler = new GetIncidentCorrelation.Handler();
        var query = new GetIncidentCorrelation.Query("a1b2c3d4-0002-0000-0000-000000000002");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Confidence.Should().Be(CorrelationConfidence.Low);
        result.Value.RelatedChanges.Should().BeEmpty();
    }

    [Fact]
    public async Task GetIncidentCorrelation_UnknownIncident_ShouldReturnNotFound()
    {
        var handler = new GetIncidentCorrelation.Handler();
        var query = new GetIncidentCorrelation.Query("nonexistent");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ── GetIncidentEvidence ──────────────────────────────────────────

    [Fact]
    public async Task GetIncidentEvidence_KnownIncident_ShouldReturnEvidence()
    {
        var handler = new GetIncidentEvidence.Handler();
        var query = new GetIncidentEvidence.Query("a1b2c3d4-0001-0000-0000-000000000001");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Observations.Should().NotBeEmpty();
        result.Value.OperationalSignalsSummary.Should().NotBeNullOrEmpty();
        result.Value.DegradationSummary.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetIncidentEvidence_UnknownIncident_ShouldReturnNotFound()
    {
        var handler = new GetIncidentEvidence.Handler();
        var query = new GetIncidentEvidence.Query("nonexistent");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ── GetIncidentMitigation ────────────────────────────────────────

    [Fact]
    public async Task GetIncidentMitigation_KnownIncident_ShouldReturnMitigation()
    {
        var handler = new GetIncidentMitigation.Handler();
        var query = new GetIncidentMitigation.Query("a1b2c3d4-0001-0000-0000-000000000001");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MitigationStatus.Should().Be(MitigationStatus.InProgress);
        result.Value.SuggestedActions.Should().NotBeEmpty();
        result.Value.RecommendedRunbooks.Should().NotBeEmpty();
        result.Value.RollbackRelevant.Should().BeTrue();
    }

    [Fact]
    public async Task GetIncidentMitigation_ExternalFailure_ShouldNotRecommendRollback()
    {
        var handler = new GetIncidentMitigation.Handler();
        var query = new GetIncidentMitigation.Query("a1b2c3d4-0002-0000-0000-000000000002");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MitigationStatus.Should().Be(MitigationStatus.NotStarted);
        result.Value.RollbackRelevant.Should().BeFalse();
    }

    [Fact]
    public async Task GetIncidentMitigation_UnknownIncident_ShouldReturnNotFound()
    {
        var handler = new GetIncidentMitigation.Handler();
        var query = new GetIncidentMitigation.Query("nonexistent");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ── GetIncidentSummary ───────────────────────────────────────────

    [Fact]
    public async Task GetIncidentSummary_ShouldReturnAggregatedMetrics()
    {
        var handler = new GetIncidentSummary.Handler();
        var query = new GetIncidentSummary.Query(null, null, null, null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalOpen.Should().BeGreaterThan(0);
        result.Value.CriticalIncidents.Should().BeGreaterThanOrEqualTo(0);
        result.Value.ServicesImpacted.Should().BeGreaterThan(0);
        result.Value.SeverityBreakdown.Should().NotBeNull();
        result.Value.StatusBreakdown.Should().NotBeNull();
    }

    [Fact]
    public void GetIncidentSummary_Validator_ShouldAcceptEmptyFilters()
    {
        var validator = new GetIncidentSummary.Validator();
        var query = new GetIncidentSummary.Query(null, null, null, null);

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    // ── ListIncidentsByService ───────────────────────────────────────

    [Fact]
    public async Task ListIncidentsByService_KnownService_ShouldReturnItems()
    {
        var handler = new ListIncidentsByService.Handler();
        var query = new ListIncidentsByService.Query("svc-payment-gateway", null, 1, 100);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceId.Should().Be("svc-payment-gateway");
        result.Value.Items.Should().AllSatisfy(i => i.ServiceId.Should().Be("svc-payment-gateway"));
    }

    [Fact]
    public async Task ListIncidentsByService_UnknownService_ShouldReturnEmpty()
    {
        var handler = new ListIncidentsByService.Handler();
        var query = new ListIncidentsByService.Query("svc-nonexistent", null, 1, 100);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public void ListIncidentsByService_Validator_ShouldRejectEmptyServiceId()
    {
        var validator = new ListIncidentsByService.Validator();
        var query = new ListIncidentsByService.Query("", null, 1, 20);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    // ── ListIncidentsByTeam ──────────────────────────────────────────

    [Fact]
    public async Task ListIncidentsByTeam_KnownTeam_ShouldReturnItems()
    {
        var handler = new ListIncidentsByTeam.Handler();
        var query = new ListIncidentsByTeam.Query("order-squad", null, 1, 100);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TeamId.Should().Be("order-squad");
        result.Value.Items.Should().AllSatisfy(i => i.OwnerTeam.Should().Be("order-squad"));
    }

    [Fact]
    public async Task ListIncidentsByTeam_UnknownTeam_ShouldReturnEmpty()
    {
        var handler = new ListIncidentsByTeam.Handler();
        var query = new ListIncidentsByTeam.Query("unknown-squad", null, 1, 100);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public void ListIncidentsByTeam_Validator_ShouldRejectEmptyTeamId()
    {
        var validator = new ListIncidentsByTeam.Validator();
        var query = new ListIncidentsByTeam.Query("", null, 1, 20);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }
}
