using FluentAssertions;

using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.SelectMitigationPlaybook;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents;

using NSubstitute;

using Xunit;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Application;

/// <summary>
/// Testes da feature SelectMitigationPlaybook (Phase 3.4 — Mitigation Playbook auto-selection):
///   - Seleciona o runbook mais adequado para o incidente
///   - Retorna PlaybookFound=false quando não há runbooks
///   - Devolve contexto de execução com urgência baseada na severidade
///   - Valida o identificador de incidente
/// </summary>
public sealed class MitigationPlaybookSelectionTests
{
    private readonly InMemoryIncidentStore _store = new();
    private readonly IRunbookRepository _runbookRepository = Substitute.For<IRunbookRepository>();

    private SelectMitigationPlaybook.Handler CreateHandler()
        => new(_store, _runbookRepository);

    private string GetFirstIncidentId()
    {
        var items = _store.GetIncidentListItems();
        items.Should().NotBeEmpty("InMemoryIncidentStore should have seeded incidents");
        return items[0].IncidentId.ToString();
    }

    [Fact]
    public async Task SelectMitigationPlaybook_NoMatchingRunbooks_ShouldReturnPlaybookNotFound()
    {
        var incidentId = GetFirstIncidentId();
        _runbookRepository
            .ListAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new List<RunbookRecord>().AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(
            new SelectMitigationPlaybook.Query(incidentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PlaybookFound.Should().BeFalse();
        result.Value.SelectedRunbookId.Should().BeNull();
        result.Value.SelectionRationale.Should().Contain("No matching runbook");
        result.Value.ExecutionUrgency.Should().NotBeNullOrEmpty();
        result.Value.ExecutionContext.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SelectMitigationPlaybook_WithMatchingRunbook_ShouldReturnSelectedPlaybook()
    {
        var incidentId = GetFirstIncidentId();
        var now = DateTimeOffset.UtcNow;
        var runbooks = new List<RunbookRecord>
        {
            RunbookRecord.Create(
                RunbookRecordId.New(),
                "Service Degradation Rollback",
                "Steps to rollback a degraded service.",
                linkedService: null,
                linkedIncidentType: "ServiceDegradation",
                stepsJson: null,
                prerequisitesJson: null,
                postNotes: null,
                maintainedBy: "platform-team",
                publishedAt: now)
        };

        _runbookRepository
            .ListAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(runbooks.AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(
            new SelectMitigationPlaybook.Query(incidentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PlaybookFound.Should().BeTrue();
        result.Value.SelectedRunbookId.Should().NotBeNull();
        result.Value.SelectedRunbookTitle.Should().Be("Service Degradation Rollback");
        result.Value.ExecutionUrgency.Should().NotBeNullOrEmpty();
        result.Value.ExecutionContext.Should().NotBeEmpty();
        result.Value.SelectionRationale.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SelectMitigationPlaybook_UnknownIncident_ShouldReturnNotFound()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(
            new SelectMitigationPlaybook.Query("UNKNOWN-INC-99999"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task SelectMitigationPlaybook_Validator_EmptyId_ShouldFail()
    {
        var validator = new SelectMitigationPlaybook.Validator();

        var result = await validator.ValidateAsync(new SelectMitigationPlaybook.Query(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IncidentId");
    }

    [Fact]
    public async Task SelectMitigationPlaybook_Validator_ValidId_ShouldPass()
    {
        var validator = new SelectMitigationPlaybook.Validator();

        var result = await validator.ValidateAsync(new SelectMitigationPlaybook.Query("INC-2026-0001"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task SelectMitigationPlaybook_WithMultipleRunbooks_ShouldReturnBestMatch()
    {
        var incidentId = GetFirstIncidentId();
        var now = DateTimeOffset.UtcNow;
        var runbooks = new List<RunbookRecord>
        {
            RunbookRecord.Create(
                RunbookRecordId.New(),
                "Generic Rollback",
                "Generic rollback procedure.",
                linkedService: null,
                linkedIncidentType: null,
                stepsJson: null,
                prerequisitesJson: null,
                postNotes: null,
                maintainedBy: "team",
                publishedAt: now),
            RunbookRecord.Create(
                RunbookRecordId.New(),
                "Specific Service Degradation Runbook",
                "Specific runbook for service degradation.",
                linkedService: null,
                linkedIncidentType: "ServiceDegradation",
                stepsJson: null,
                prerequisitesJson: null,
                postNotes: null,
                maintainedBy: "team",
                publishedAt: now)
        };

        _runbookRepository
            .ListAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(runbooks.AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(
            new SelectMitigationPlaybook.Query(incidentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PlaybookFound.Should().BeTrue();
        // O runbook com LinkedIncidentType deve ter maior score
        result.Value.SelectedRunbookTitle.Should().Be("Specific Service Degradation Runbook");
    }
}
