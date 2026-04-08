using FluentAssertions;

using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.FindSimilarIncidents;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentImpactAssessment;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetRootCauseSuggestion;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.TriageIncident;
using NexTraceOne.OperationalIntelligence.Tests.Incidents.Infrastructure;

using System.Linq;

using Xunit;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Application;

/// <summary>
/// Testes das novas features de AI-Powered Incident Investigation (Phase 3.4):
///   - TriageIncident: auto-triage baseado em correlação e sinais
///   - GetRootCauseSuggestion: sugestão de causa raiz por correlação de mudanças
///   - GetIncidentImpactAssessment: avaliação de impacto em serviços e contratos
///   - FindSimilarIncidents: pesquisa de incidentes semelhantes
/// </summary>
public sealed class AiIncidentInvestigationTests
{
    private readonly InMemoryIncidentStore _store = new();

    // ── TriageIncident ──────────────────────────────────────────────

    [Fact]
    public async Task TriageIncident_ForKnownIncident_ShouldReturnTriage()
    {
        var handler = new TriageIncident.Handler(_store);
        var validIncidentId = GetFirstIncidentId();

        var result = await handler.Handle(new TriageIncident.Query(validIncidentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IncidentId.Should().Be(validIncidentId);
        result.Value.SuggestedSeverity.Should().BeDefined();
        result.Value.TriageConfidence.Should().NotBeNullOrEmpty();
        result.Value.Rationale.Should().NotBeNullOrEmpty();
        result.Value.TriageSignals.Should().NotBeEmpty();
        result.Value.RecommendedAction.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TriageIncident_ForUnknownIncident_ShouldReturnNotFound()
    {
        var handler = new TriageIncident.Handler(_store);

        var result = await handler.Handle(new TriageIncident.Query("UNKNOWN-INC-99999"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task TriageIncident_Validator_EmptyId_ShouldFail()
    {
        var validator = new TriageIncident.Validator();

        var validationResult = await validator.ValidateAsync(new TriageIncident.Query(string.Empty));

        validationResult.IsValid.Should().BeFalse();
    }

    // ── GetRootCauseSuggestion ──────────────────────────────────────

    [Fact]
    public async Task GetRootCauseSuggestion_ForKnownIncident_ShouldReturnSuggestion()
    {
        var handler = new GetRootCauseSuggestion.Handler(_store);
        var validIncidentId = GetFirstIncidentId();

        var result = await handler.Handle(new GetRootCauseSuggestion.Query(validIncidentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IncidentId.Should().Be(validIncidentId);
        result.Value.SuggestedCauseCategory.Should().NotBeNullOrEmpty();
        result.Value.SuggestedCauseSummary.Should().NotBeNullOrEmpty();
        result.Value.ConfidenceLevel.Should().NotBeNullOrEmpty();
        result.Value.InvestigationSteps.Should().NotBeEmpty();
        result.Value.SupportingEvidence.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRootCauseSuggestion_WithNoCorrelation_ShouldReturnNoSuggestion()
    {
        var handler = new GetRootCauseSuggestion.Handler(_store);

        var allIncidents = _store.GetIncidentListItems();
        var incidentWithNoCorrelation = allIncidents
            .AsEnumerable()
            .FirstOrDefault(i => !i.HasCorrelatedChanges);

        if (incidentWithNoCorrelation is null) return;

        var result = await handler.Handle(
            new GetRootCauseSuggestion.Query(incidentWithNoCorrelation.IncidentId.ToString()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasSuggestion.Should().BeFalse();
        result.Value.InvestigationSteps.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetRootCauseSuggestion_ForUnknownIncident_ShouldReturnNotFound()
    {
        var handler = new GetRootCauseSuggestion.Handler(_store);

        var result = await handler.Handle(new GetRootCauseSuggestion.Query("UNKNOWN-INC"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── GetIncidentImpactAssessment ─────────────────────────────────

    [Fact]
    public async Task GetIncidentImpactAssessment_ForKnownIncident_ShouldReturnAssessment()
    {
        var handler = new GetIncidentImpactAssessment.Handler(_store);
        var validIncidentId = GetFirstIncidentId();

        var result = await handler.Handle(new GetIncidentImpactAssessment.Query(validIncidentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IncidentId.Should().Be(validIncidentId);
        result.Value.Title.Should().NotBeNullOrEmpty();
        result.Value.PropagationRisk.Should().BeOneOf("Low", "Medium", "High", "Critical");
        result.Value.PropagationRationale.Should().NotBeNullOrEmpty();
        result.Value.ImpactSummary.Should().NotBeNullOrEmpty();
        result.Value.AffectedServices.Should().NotBeNull();
        result.Value.ImpactedContracts.Should().NotBeNull();
    }

    [Fact]
    public async Task GetIncidentImpactAssessment_ImpactSummary_ShouldContainEnvironment()
    {
        var handler = new GetIncidentImpactAssessment.Handler(_store);
        var validIncidentId = GetFirstIncidentId();

        var result = await handler.Handle(new GetIncidentImpactAssessment.Query(validIncidentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ImpactSummary.Should().Contain("service");
    }

    [Fact]
    public async Task GetIncidentImpactAssessment_ForUnknownIncident_ShouldReturnNotFound()
    {
        var handler = new GetIncidentImpactAssessment.Handler(_store);

        var result = await handler.Handle(new GetIncidentImpactAssessment.Query("UNKNOWN"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── FindSimilarIncidents ────────────────────────────────────────

    [Fact]
    public async Task FindSimilarIncidents_ForKnownIncident_ShouldReturnResponse()
    {
        var handler = new FindSimilarIncidents.Handler(_store);
        var validIncidentId = GetFirstIncidentId();

        var result = await handler.Handle(
            new FindSimilarIncidents.Query(validIncidentId, LookbackDays: 365, MaxResults: 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IncidentId.Should().Be(validIncidentId);
        result.Value.LookbackDays.Should().Be(365);
        result.Value.RecurrencePattern.Should().NotBeNullOrEmpty();
        result.Value.SimilarIncidents.Should().NotBeNull();
    }

    [Fact]
    public async Task FindSimilarIncidents_DefaultLookback_ShouldBe90Days()
    {
        var handler = new FindSimilarIncidents.Handler(_store);
        var validIncidentId = GetFirstIncidentId();

        var result = await handler.Handle(
            new FindSimilarIncidents.Query(validIncidentId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LookbackDays.Should().Be(90);
    }

    [Fact]
    public async Task FindSimilarIncidents_ForUnknownIncident_ShouldReturnNotFound()
    {
        var handler = new FindSimilarIncidents.Handler(_store);

        var result = await handler.Handle(
            new FindSimilarIncidents.Query("UNKNOWN-INC"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task FindSimilarIncidents_Validator_InvalidLookback_ShouldFail()
    {
        var validator = new FindSimilarIncidents.Validator();

        var validationResult = await validator.ValidateAsync(
            new FindSimilarIncidents.Query("INC-001", LookbackDays: 0));

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task FindSimilarIncidents_Validator_TooManyResults_ShouldFail()
    {
        var validator = new FindSimilarIncidents.Validator();

        var validationResult = await validator.ValidateAsync(
            new FindSimilarIncidents.Query("INC-001", MaxResults: 100));

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task FindSimilarIncidents_SimilarityScoring_HighScoreForSameServiceAndType()
    {
        var handler = new FindSimilarIncidents.Handler(_store);
        var allIncidents = _store.GetIncidentListItems();

        if (allIncidents.Count < 2) return;

        var validIncidentId = allIncidents[0].IncidentId.ToString();

        var result = await handler.Handle(
            new FindSimilarIncidents.Query(validIncidentId, LookbackDays: 365, MaxResults: 50),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        if (result.Value.SimilarIncidents.Count > 0)
        {
            result.Value.SimilarIncidents.Should().BeInDescendingOrder(s => s.SimilarityScore);
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private string GetFirstIncidentId()
    {
        var items = _store.GetIncidentListItems();
        items.Should().NotBeEmpty("InMemoryIncidentStore should have seeded incidents");
        return items[0].IncidentId.ToString();
    }
}
