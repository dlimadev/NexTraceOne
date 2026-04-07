using System.Text.Json;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentCorrelation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentDetail;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentEvidence;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentMitigation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationRecommendations;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationValidation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidents;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.UpdateMitigationWorkflowAction;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents;

/// <summary>
/// Implementação in-memory do IIncidentStore para utilização em testes unitários.
/// Carrega os mesmos dados de seed que são usados na migração inicial, garantindo
/// paridade funcional com o EfIncidentStore em cenários de teste.
/// NÃO é registada em DI de produção — apenas EfIncidentStore é o provider ativo.
/// </summary>
[System.Obsolete("Test-only implementation. Production uses EfIncidentStore registered in DI. This class will be removed in a future version.")]
internal sealed class InMemoryIncidentStore : IIncidentStore
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly List<IncidentRecord> _incidents;
    private readonly List<MitigationWorkflowRecord> _workflows;
    private readonly List<MitigationWorkflowActionLog> _actionLogs;
    private readonly List<MitigationValidationLog> _validationLogs;

    public InMemoryIncidentStore()
    {
        var now = DateTimeOffset.UtcNow;
        _incidents = new List<IncidentRecord>(IncidentSeedData.GetIncidents(now));
        _workflows = new List<MitigationWorkflowRecord>(IncidentSeedData.GetWorkflows());
        _actionLogs = new List<MitigationWorkflowActionLog>(IncidentSeedData.GetWorkflowActionLogs());
        _validationLogs = new List<MitigationValidationLog>(IncidentSeedData.GetValidationLogs());
    }

    // ── Incidents ────────────────────────────────────────────────────────

    public CreateIncidentResult CreateIncident(CreateIncidentInput input)
    {
        var now = DateTimeOffset.UtcNow;
        var incidentId = IncidentRecordId.New();
        var reference = $"INC-{now.UtcDateTime.Year}-{(_incidents.Count + 1):0000}";

        var incident = IncidentRecord.Create(
            incidentId, reference, input.Title, input.Description,
            input.IncidentType, input.Severity, IncidentStatus.Open,
            input.ServiceId, input.ServiceDisplayName, input.OwnerTeam,
            input.ImpactedDomain, input.Environment,
            input.DetectedAtUtc ?? now, now,
            hasCorrelation: false,
            correlationConfidence: CorrelationConfidence.NotAssessed,
            mitigationStatus: MitigationStatus.NotStarted);

        _incidents.Add(incident);
        return new CreateIncidentResult(incidentId.Value, reference, now);
    }

    public IncidentCorrelationContext? GetIncidentCorrelationContext(string incidentId)
    {
        var incident = FindIncident(incidentId);
        if (incident is null) return null;

        return new IncidentCorrelationContext(
            incident.Id.Value, incident.ServiceId, incident.ServiceName,
            incident.Environment, incident.DetectedAt);
    }

    public void SaveIncidentCorrelation(string incidentId, GetIncidentCorrelation.Response correlation)
    {
        // No-op for in-memory tests — correlation data is pre-seeded
    }

    public bool IncidentExists(string incidentId) => FindIncident(incidentId) is not null;

    public IReadOnlyList<ListIncidents.IncidentListItem> GetIncidentListItems()
    {
        return _incidents
            .OrderByDescending(i => i.DetectedAt)
            .Select(i => new ListIncidents.IncidentListItem(
                i.Id.Value, i.ExternalRef, i.Title, i.Type, i.Severity, i.Status,
                i.ServiceId, i.ServiceName, i.OwnerTeam, i.Environment, i.DetectedAt,
                i.HasCorrelation, i.CorrelationConfidence, i.MitigationStatus))
            .ToList();
    }

    public GetIncidentDetail.Response? GetIncidentDetail(string incidentId)
    {
        var incident = FindIncident(incidentId);
        if (incident is null) return null;

        var identity = new GetIncidentDetail.IncidentIdentity(
            incident.Id.Value, incident.ExternalRef, incident.Title, incident.Description,
            incident.Type, incident.Severity, incident.Status,
            incident.DetectedAt, incident.LastUpdatedAt);

        var linkedServices = Deserialize<List<LinkedServiceDto>>(incident.LinkedServicesJson)
            ?.Select(s => new GetIncidentDetail.LinkedServiceItem(s.ServiceId, s.DisplayName, s.ServiceType, s.Criticality))
            .ToArray() ?? [];

        var timeline = Deserialize<List<TimelineEntryDto>>(incident.TimelineJson)
            ?.Select(t => new GetIncidentDetail.TimelineEntry(t.Timestamp, t.Description))
            .ToArray() ?? [];

        var correlatedChanges = Deserialize<List<CorrelatedChangeDto>>(incident.CorrelatedChangesJson)
            ?.Select(c => new GetIncidentDetail.RelatedChangeItem(c.ChangeId, c.Description, c.ChangeType, c.ConfidenceStatus, c.DeployedAt))
            .ToArray() ?? [];

        var correlatedServices = Deserialize<List<CorrelatedServiceDto>>(incident.CorrelatedServicesJson)
            ?.Select(s => new GetIncidentDetail.RelatedServiceItem(s.ServiceId, s.DisplayName, s.ImpactDescription))
            .ToArray() ?? [];

        var evidenceObservations = Deserialize<List<EvidenceObservationDto>>(incident.EvidenceObservationsJson)
            ?.Select(e => new GetIncidentDetail.EvidenceItem(e.Title, e.Description))
            .ToArray() ?? [];

        var relatedContracts = Deserialize<List<RelatedContractDto>>(incident.RelatedContractsJson)
            ?.Select(c => new GetIncidentDetail.RelatedContractItem(c.ContractVersionId, c.Name, c.Version, c.Protocol, c.LifecycleState))
            .ToArray() ?? [];

        var runbooks = Deserialize<List<RunbookLinkDto>>(incident.RunbookLinksJson)
            ?.Select(r => new GetIncidentDetail.RunbookItem(r.Title, r.Url))
            .ToArray() ?? [];

        var mitigationActions = Deserialize<List<MitigationActionDto>>(incident.MitigationActionsJson)
            ?.Select(a => new GetIncidentDetail.MitigationActionItem(a.Description, a.Status, a.Completed))
            .ToArray() ?? [];

        var correlation = new GetIncidentDetail.CorrelationSummary(
            incident.CorrelationConfidence,
            incident.CorrelationAnalysis ?? string.Empty,
            correlatedChanges, correlatedServices);

        var evidence = new GetIncidentDetail.EvidenceSummary(
            incident.EvidenceTelemetrySummary ?? string.Empty,
            incident.EvidenceBusinessImpact ?? string.Empty,
            evidenceObservations);

        var mitigation = new GetIncidentDetail.MitigationSummary(
            incident.MitigationStatus, mitigationActions,
            incident.MitigationNarrative,
            incident.HasEscalationPath, incident.EscalationPath);

        return new GetIncidentDetail.Response(
            identity, linkedServices, incident.OwnerTeam,
            incident.ImpactedDomain ?? string.Empty, incident.Environment,
            timeline, correlation, evidence, relatedContracts, runbooks, mitigation);
    }

    public GetIncidentCorrelation.Response? GetIncidentCorrelation(string incidentId)
    {
        var incident = FindIncident(incidentId);
        if (incident is null) return null;

        var changes = Deserialize<List<CorrelatedChangeDto>>(incident.CorrelatedChangesJson)
            ?.Select(c => new GetIncidentCorrelation.CorrelatedChange(c.ChangeId, c.Description, c.ChangeType, c.ConfidenceStatus, c.DeployedAt))
            .ToArray() ?? [];

        var services = Deserialize<List<CorrelatedServiceDto>>(incident.CorrelatedServicesJson)
            ?.Select(s => new GetIncidentCorrelation.CorrelatedService(s.ServiceId, s.DisplayName, s.ImpactDescription))
            .ToArray() ?? [];

        var dependencies = Deserialize<List<CorrelatedDependencyDto>>(incident.CorrelatedDependenciesJson)
            ?.Select(d => new GetIncidentCorrelation.CorrelatedDependency(d.ServiceId, d.DisplayName, d.Relationship))
            .ToArray() ?? [];

        var contracts = Deserialize<List<ImpactedContractDto>>(incident.ImpactedContractsJson)
            ?.Select(c => new GetIncidentCorrelation.ImpactedContract(c.ContractVersionId, c.Name, c.Version, c.Protocol))
            .ToArray() ?? [];

        return new GetIncidentCorrelation.Response(
            incident.Id.Value, incident.CorrelationConfidence,
            DeriveScore(incident.CorrelationConfidence),
            incident.CorrelationAnalysis ?? string.Empty,
            changes, services, dependencies, contracts);
    }

    public GetIncidentEvidence.Response? GetIncidentEvidence(string incidentId)
    {
        var incident = FindIncident(incidentId);
        if (incident is null) return null;

        var observations = Deserialize<List<EvidenceObservationDto>>(incident.EvidenceObservationsJson)
            ?.Select(e => new GetIncidentEvidence.EvidenceObservation(e.Title, e.Description))
            .ToArray() ?? [];

        return new GetIncidentEvidence.Response(
            incident.Id.Value,
            incident.EvidenceTelemetrySummary ?? string.Empty,
            incident.EvidenceBusinessImpact ?? string.Empty,
            observations,
            incident.EvidenceAnalysis ?? string.Empty,
            incident.EvidenceTemporalContext);
    }

    public GetIncidentMitigation.Response? GetIncidentMitigation(string incidentId)
    {
        var incident = FindIncident(incidentId);
        if (incident is null) return null;

        var actions = Deserialize<List<MitigationActionDto>>(incident.MitigationActionsJson)
            ?.Select(a => new GetIncidentMitigation.SuggestedAction(a.Description, a.Status, a.Completed))
            .ToArray() ?? [];

        var runbooks = Deserialize<List<MitigationRunbookDto>>(incident.MitigationRecommendedRunbooksJson)
            ?.Select(r => new GetIncidentMitigation.RecommendedRunbook(r.Title, r.Url, r.Description))
            .ToArray() ?? [];

        return new GetIncidentMitigation.Response(
            incident.Id.Value, incident.MitigationStatus,
            actions, runbooks, incident.MitigationNarrative,
            incident.HasEscalationPath, incident.EscalationPath);
    }

    // ── Mitigation Workflows ─────────────────────────────────────────────

    public GetMitigationRecommendations.Response? GetMitigationRecommendations(string incidentId)
    {
        var incident = FindIncident(incidentId);
        if (incident is null) return null;

        var recommendations = Deserialize<List<MitigationRecommendationDto>>(incident.MitigationRecommendationsJson)
            ?.Select(r => new GetMitigationRecommendations.MitigationRecommendationDto(
                r.RecommendationId, r.Title, r.Summary, r.RecommendedActionType,
                r.RationaleSummary, r.EvidenceSummary, r.RequiresApproval, r.RiskLevel,
                r.LinkedRunbookIds ?? [], r.SuggestedValidationSteps ?? []))
            .ToArray() ?? [];

        return new GetMitigationRecommendations.Response(incident.Id.Value, recommendations);
    }

    public GetMitigationWorkflow.Response? GetMitigationWorkflow(string incidentId, string workflowId)
    {
        if (!Guid.TryParse(workflowId, out var wfGuid))
            return null;

        var workflow = _workflows.SingleOrDefault(w =>
            w.Id == MitigationWorkflowRecordId.From(wfGuid) && w.IncidentId == incidentId);

        if (workflow is null)
            return null;

        var steps = Deserialize<List<WorkflowStepDto>>(workflow.StepsJson)
            ?.Select(s => new GetMitigationWorkflow.WorkflowStepDto(
                s.StepOrder, s.Title, s.Description, s.IsCompleted,
                s.CompletedBy, s.CompletedAt, s.Notes))
            .ToArray() ?? [];

        var decisions = Deserialize<List<WorkflowDecisionDto>>(workflow.DecisionsJson)
            ?.Select(d => new GetMitigationWorkflow.WorkflowDecisionDto(
                d.DecisionType, d.DecidedBy, d.DecidedAt, d.Reason))
            .ToArray() ?? [];

        return new GetMitigationWorkflow.Response(
            workflow.Id.Value,
            Guid.TryParse(workflow.IncidentId, out var incGuid) ? incGuid : Guid.Empty,
            workflow.Title,
            workflow.Status, workflow.ActionType, workflow.RiskLevel,
            workflow.RequiresApproval,
            workflow.ApprovedBy, workflow.ApprovedAt,
            workflow.CreatedByUser, workflow.CreatedAt,
            workflow.StartedAt, workflow.CompletedAt,
            workflow.CompletedOutcome, workflow.CompletedNotes,
            workflow.LinkedRunbookId,
            steps, decisions);
    }

    public UpdateMitigationWorkflowAction.Response? UpdateMitigationWorkflowAction(
        string incidentId, string workflowId, string action,
        MitigationWorkflowStatus newStatus, string? performedBy,
        string? reason, string? notes)
    {
        var wfGuid = Guid.TryParse(workflowId, out var parsed) ? parsed : Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var actionLog = MitigationWorkflowActionLog.Create(
            MitigationWorkflowActionLogId.New(),
            wfGuid, incidentId, action, newStatus,
            performedBy, reason, notes, now);
        _actionLogs.Add(actionLog);

        var workflow = _workflows.SingleOrDefault(w =>
            w.Id == MitigationWorkflowRecordId.From(wfGuid) && w.IncidentId == incidentId);
        workflow?.UpdateStatus(newStatus);

        return new UpdateMitigationWorkflowAction.Response(wfGuid, newStatus, action, now);
    }

    public GetMitigationValidation.Response? GetMitigationValidation(string incidentId, string workflowId)
    {
        if (!Guid.TryParse(workflowId, out var wfGuid))
            return null;

        var validation = _validationLogs
            .Where(v => v.IncidentId == incidentId && v.WorkflowId == wfGuid)
            .OrderByDescending(v => v.ValidatedAt)
            .FirstOrDefault();

        if (validation is null)
            return null;

        var checks = Deserialize<List<ValidationCheckDto>>(validation.ChecksJson)
            ?.Select(c => new GetMitigationValidation.ValidationCheckDto(c.CheckName, null, c.IsPassed, c.ObservedValue))
            .ToArray() ?? [];

        return new GetMitigationValidation.Response(
            wfGuid, validation.Status, checks,
            validation.ObservedOutcome,
            validation.ObservedOutcome,
            validation.ValidatedAt, validation.ValidatedBy);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private IncidentRecord? FindIncident(string incidentId)
    {
        if (!Guid.TryParse(incidentId, out var guid))
            return null;

        return _incidents.SingleOrDefault(i => i.Id == IncidentRecordId.From(guid));
    }

    private static T? Deserialize<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOpts);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static decimal DeriveScore(CorrelationConfidence confidence) => confidence switch
    {
        CorrelationConfidence.Confirmed => 95m,
        CorrelationConfidence.High => 80m,
        CorrelationConfidence.Medium => 55m,
        CorrelationConfidence.Low => 25m,
        _ => 0m
    };

    // ── Internal JSON DTOs ───────────────────────────────────────────────

    private sealed class LinkedServiceDto
    {
        public string ServiceId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string Criticality { get; set; } = string.Empty;
    }

    private sealed class TimelineEntryDto
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    private sealed class CorrelatedChangeDto
    {
        public Guid ChangeId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ChangeType { get; set; } = string.Empty;
        public string ConfidenceStatus { get; set; } = string.Empty;
        public DateTimeOffset DeployedAt { get; set; }
    }

    private sealed class CorrelatedServiceDto
    {
        public string ServiceId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ImpactDescription { get; set; } = string.Empty;
    }

    private sealed class CorrelatedDependencyDto
    {
        public string ServiceId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
    }

    private sealed class ImpactedContractDto
    {
        public Guid ContractVersionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;
    }

    private sealed class EvidenceObservationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    private sealed class RelatedContractDto
    {
        public Guid ContractVersionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;
        public string LifecycleState { get; set; } = string.Empty;
    }

    private sealed class RunbookLinkDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Url { get; set; }
    }

    private sealed class MitigationActionDto
    {
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool Completed { get; set; }
    }

    private sealed class MitigationRunbookDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Description { get; set; }
    }

    private sealed class MitigationRecommendationDto
    {
        public Guid RecommendationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public MitigationActionType RecommendedActionType { get; set; }
        public string RationaleSummary { get; set; } = string.Empty;
        public string? EvidenceSummary { get; set; }
        public bool RequiresApproval { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public List<Guid>? LinkedRunbookIds { get; set; }
        public List<string>? SuggestedValidationSteps { get; set; }
    }

    private sealed class WorkflowStepDto
    {
        public int StepOrder { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsCompleted { get; set; }
        public string? CompletedBy { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class WorkflowDecisionDto
    {
        public MitigationDecisionType DecisionType { get; set; }
        public string DecidedBy { get; set; } = string.Empty;
        public DateTimeOffset DecidedAt { get; set; }
        public string? Reason { get; set; }
    }

    private sealed class ValidationCheckDto
    {
        public string CheckName { get; set; } = string.Empty;
        public bool IsPassed { get; set; }
        public string? ObservedValue { get; set; }
    }
}
