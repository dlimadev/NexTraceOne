using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateMitigationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentCorrelation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentDetail;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentEvidence;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentMitigation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationHistory;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationRecommendations;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationValidation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetRunbookDetail;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidents;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListRunbooks;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.RecordMitigationValidation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.UpdateMitigationWorkflowAction;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents;

/// <summary>
/// Implementação EF Core do IIncidentStore.
/// Consulta IncidentDbContext para todas as operações de leitura e escrita,
/// mapeando entidades de domínio para os DTOs esperados pelos handlers.
/// </summary>
internal sealed class EfIncidentStore(
    IncidentDbContext db,
    IDateTimeProvider clock,
    ICurrentUser currentUser) : IIncidentStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    // ── Incidents ────────────────────────────────────────────────────────

    public bool IncidentExists(string incidentId)
    {
        if (!Guid.TryParse(incidentId, out var guid))
            return false;

        return db.Incidents.Any(i => i.Id == IncidentRecordId.From(guid));
    }

    public IReadOnlyList<ListIncidents.IncidentListItem> GetIncidentListItems()
    {
        return db.Incidents
            .OrderByDescending(i => i.DetectedAt)
            .AsNoTracking()
            .Select(i => new ListIncidents.IncidentListItem(
                i.Id.Value,
                i.ExternalRef,
                i.Title,
                i.Type,
                i.Severity,
                i.Status,
                i.ServiceId,
                i.ServiceName,
                i.OwnerTeam,
                i.Environment,
                i.DetectedAt,
                i.HasCorrelation,
                i.CorrelationConfidence,
                i.MitigationStatus))
            .ToList();
    }

    public GetIncidentDetail.Response? GetIncidentDetail(string incidentId)
    {
        if (!Guid.TryParse(incidentId, out var guid))
            return null;

        var incident = db.Incidents
            .AsNoTracking()
            .SingleOrDefault(i => i.Id == IncidentRecordId.From(guid));

        if (incident is null)
            return null;

        var identity = new GetIncidentDetail.IncidentIdentity(
            incident.Id.Value,
            incident.ExternalRef,
            incident.Title,
            incident.Description,
            incident.Type,
            incident.Severity,
            incident.Status,
            incident.DetectedAt,
            incident.LastUpdatedAt);

        var linkedServices = Deserialize<List<LinkedServiceJson>>(incident.LinkedServicesJson)
            ?.Select(s => new GetIncidentDetail.LinkedServiceItem(s.ServiceId, s.DisplayName, s.ServiceType, s.Criticality))
            .ToArray()
            ?? [];

        var timeline = Deserialize<List<TimelineEntryJson>>(incident.TimelineJson)
            ?.Select(t => new GetIncidentDetail.TimelineEntry(t.Timestamp, t.Description))
            .ToArray()
            ?? [];

        var correlatedChanges = Deserialize<List<CorrelatedChangeJson>>(incident.CorrelatedChangesJson)
            ?.Select(c => new GetIncidentDetail.RelatedChangeItem(c.ChangeId, c.Description, c.ChangeType, c.ConfidenceStatus, c.DeployedAt))
            .ToArray()
            ?? [];

        var correlatedServices = Deserialize<List<CorrelatedServiceJson>>(incident.CorrelatedServicesJson)
            ?.Select(s => new GetIncidentDetail.RelatedServiceItem(s.ServiceId, s.DisplayName, s.ImpactDescription))
            .ToArray()
            ?? [];

        var evidenceObservations = Deserialize<List<EvidenceObservationJson>>(incident.EvidenceObservationsJson)
            ?.Select(e => new GetIncidentDetail.EvidenceItem(e.Title, e.Description))
            .ToArray()
            ?? [];

        var relatedContracts = Deserialize<List<RelatedContractJson>>(incident.RelatedContractsJson)
            ?.Select(c => new GetIncidentDetail.RelatedContractItem(c.ContractVersionId, c.Name, c.Version, c.Protocol, c.LifecycleState))
            .ToArray()
            ?? [];

        var runbooks = Deserialize<List<RunbookLinkJson>>(incident.RunbookLinksJson)
            ?.Select(r => new GetIncidentDetail.RunbookItem(r.Title, r.Url))
            .ToArray()
            ?? [];

        var mitigationActions = Deserialize<List<MitigationActionJson>>(incident.MitigationActionsJson)
            ?.Select(a => new GetIncidentDetail.MitigationActionItem(a.Description, a.Status, a.Completed))
            .ToArray()
            ?? [];

        var correlation = new GetIncidentDetail.CorrelationSummary(
            incident.CorrelationConfidence,
            incident.CorrelationAnalysis ?? string.Empty,
            correlatedChanges,
            correlatedServices);

        var evidence = new GetIncidentDetail.EvidenceSummary(
            incident.EvidenceTelemetrySummary ?? string.Empty,
            incident.EvidenceBusinessImpact ?? string.Empty,
            evidenceObservations);

        var mitigation = new GetIncidentDetail.MitigationSummary(
            incident.MitigationStatus,
            mitigationActions,
            incident.MitigationNarrative,
            incident.HasEscalationPath,
            incident.EscalationPath);

        return new GetIncidentDetail.Response(
            identity,
            linkedServices,
            incident.OwnerTeam,
            incident.ImpactedDomain ?? string.Empty,
            incident.Environment,
            timeline,
            correlation,
            evidence,
            relatedContracts,
            runbooks,
            mitigation);
    }

    public GetIncidentCorrelation.Response? GetIncidentCorrelation(string incidentId)
    {
        if (!Guid.TryParse(incidentId, out var guid))
            return null;

        var incident = db.Incidents
            .AsNoTracking()
            .SingleOrDefault(i => i.Id == IncidentRecordId.From(guid));

        if (incident is null)
            return null;

        var changes = Deserialize<List<CorrelatedChangeJson>>(incident.CorrelatedChangesJson)
            ?.Select(c => new GetIncidentCorrelation.CorrelatedChange(c.ChangeId, c.Description, c.ChangeType, c.ConfidenceStatus, c.DeployedAt))
            .ToArray()
            ?? [];

        var services = Deserialize<List<CorrelatedServiceJson>>(incident.CorrelatedServicesJson)
            ?.Select(s => new GetIncidentCorrelation.CorrelatedService(s.ServiceId, s.DisplayName, s.ImpactDescription))
            .ToArray()
            ?? [];

        var dependencies = Deserialize<List<CorrelatedDependencyJson>>(incident.CorrelatedDependenciesJson)
            ?.Select(d => new GetIncidentCorrelation.CorrelatedDependency(d.ServiceId, d.DisplayName, d.Relationship))
            .ToArray()
            ?? [];

        var contracts = Deserialize<List<ImpactedContractJson>>(incident.ImpactedContractsJson)
            ?.Select(c => new GetIncidentCorrelation.ImpactedContract(c.ContractVersionId, c.Name, c.Version, c.Protocol))
            .ToArray()
            ?? [];

        return new GetIncidentCorrelation.Response(
            incident.Id.Value,
            incident.CorrelationConfidence,
            incident.CorrelationAnalysis ?? string.Empty,
            changes, services, dependencies, contracts);
    }

    public GetIncidentEvidence.Response? GetIncidentEvidence(string incidentId)
    {
        if (!Guid.TryParse(incidentId, out var guid))
            return null;

        var incident = db.Incidents
            .AsNoTracking()
            .SingleOrDefault(i => i.Id == IncidentRecordId.From(guid));

        if (incident is null)
            return null;

        var observations = Deserialize<List<EvidenceObservationJson>>(incident.EvidenceObservationsJson)
            ?.Select(e => new GetIncidentEvidence.EvidenceObservation(e.Title, e.Description))
            .ToArray()
            ?? [];

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
        if (!Guid.TryParse(incidentId, out var guid))
            return null;

        var incident = db.Incidents
            .AsNoTracking()
            .SingleOrDefault(i => i.Id == IncidentRecordId.From(guid));

        if (incident is null)
            return null;

        var actions = Deserialize<List<MitigationActionJson>>(incident.MitigationActionsJson)
            ?.Select(a => new GetIncidentMitigation.SuggestedAction(a.Description, a.Status, a.Completed))
            .ToArray()
            ?? [];

        var runbooks = Deserialize<List<MitigationRunbookJson>>(incident.MitigationRecommendedRunbooksJson)
            ?.Select(r => new GetIncidentMitigation.RecommendedRunbook(r.Title, r.Url, r.Description))
            .ToArray()
            ?? [];

        return new GetIncidentMitigation.Response(
            incident.Id.Value,
            incident.MitigationStatus,
            actions, runbooks,
            incident.MitigationNarrative,
            incident.HasEscalationPath,
            incident.EscalationPath);
    }

    // ── Mitigation Workflows ─────────────────────────────────────────────

    public GetMitigationRecommendations.Response? GetMitigationRecommendations(string incidentId)
    {
        if (!Guid.TryParse(incidentId, out var guid))
            return null;

        var incident = db.Incidents
            .AsNoTracking()
            .SingleOrDefault(i => i.Id == IncidentRecordId.From(guid));

        if (incident is null)
            return null;

        var recommendations = Deserialize<List<MitigationRecommendationJson>>(incident.MitigationRecommendationsJson)
            ?.Select(r => new GetMitigationRecommendations.MitigationRecommendationDto(
                r.RecommendationId, r.Title, r.Summary, r.RecommendedActionType,
                r.RationaleSummary, r.EvidenceSummary, r.RequiresApproval, r.RiskLevel,
                r.LinkedRunbookIds ?? [], r.SuggestedValidationSteps ?? []))
            .ToArray()
            ?? [];

        return new GetMitigationRecommendations.Response(incident.Id.Value, recommendations);
    }

    public GetMitigationWorkflow.Response? GetMitigationWorkflow(string incidentId, string workflowId)
    {
        if (!Guid.TryParse(workflowId, out var wfGuid))
            return null;

        var workflow = db.MitigationWorkflows
            .AsNoTracking()
            .SingleOrDefault(w => w.Id == MitigationWorkflowRecordId.From(wfGuid)
                && w.IncidentId == incidentId);

        if (workflow is null)
            return null;

        var steps = Deserialize<List<WorkflowStepJson>>(workflow.StepsJson)
            ?.Select(s => new GetMitigationWorkflow.WorkflowStepDto(
                s.StepOrder, s.Title, s.Description, s.IsCompleted,
                s.CompletedBy, s.CompletedAt, s.Notes))
            .ToArray()
            ?? [];

        var decisions = Deserialize<List<WorkflowDecisionJson>>(workflow.DecisionsJson)
            ?.Select(d => new GetMitigationWorkflow.WorkflowDecisionDto(
                d.DecisionType, d.DecidedBy, d.DecidedAt, d.Reason))
            .ToArray()
            ?? [];

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

    public CreateMitigationWorkflow.Response CreateMitigationWorkflow(
        string incidentId, string title, MitigationActionType actionType,
        RiskLevel riskLevel, bool requiresApproval, Guid? linkedRunbookId,
        IReadOnlyList<CreateMitigationWorkflow.CreateStepDto>? steps)
    {
        var wfId = MitigationWorkflowRecordId.New();

        string? stepsJson = null;
        if (steps is { Count: > 0 })
        {
            var jsonSteps = steps.Select(s => new WorkflowStepJson
            {
                StepOrder = s.StepOrder,
                Title = s.Title,
                Description = s.Description,
                IsCompleted = false,
            }).ToList();
            stepsJson = Serialize(jsonSteps);
        }

        var workflow = MitigationWorkflowRecord.Create(
            wfId, incidentId, title,
            MitigationWorkflowStatus.Draft, actionType, riskLevel,
            requiresApproval, "user",
            linkedRunbookId, stepsJson);

        db.MitigationWorkflows.Add(workflow);
        db.SaveChanges();

        return new CreateMitigationWorkflow.Response(wfId.Value, MitigationWorkflowStatus.Draft, workflow.CreatedAt);
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
        db.MitigationWorkflowActions.Add(actionLog);

        var workflow = db.MitigationWorkflows
            .SingleOrDefault(w => w.Id == MitigationWorkflowRecordId.From(wfGuid)
                && w.IncidentId == incidentId);
        workflow?.UpdateStatus(newStatus);

        db.SaveChanges();

        return new UpdateMitigationWorkflowAction.Response(wfGuid, newStatus, action, now);
    }

    public GetMitigationHistory.Response? GetMitigationHistory(string incidentId)
    {
        if (!Guid.TryParse(incidentId, out var guid))
            return null;

        if (!db.Incidents.Any(i => i.Id == IncidentRecordId.From(guid)))
            return null;

        var actions = db.MitigationWorkflowActions
            .Where(a => a.IncidentId == incidentId)
            .OrderBy(a => a.PerformedAt)
            .AsNoTracking()
            .ToList();

        var entries = actions.Select(a => new GetMitigationHistory.MitigationAuditEntryDto(
            a.Id.Value,
            a.WorkflowId,
            a.Action,
            a.PerformedBy ?? "system",
            a.PerformedAt,
            a.Notes,
            null,
            a.Reason,
            Array.Empty<string>()
        )).ToArray();

        return new GetMitigationHistory.Response(guid, entries);
    }

    public GetMitigationValidation.Response? GetMitigationValidation(string incidentId, string workflowId)
    {
        if (!Guid.TryParse(workflowId, out var wfGuid))
            return null;

        // Check recorded validations first (most recent)
        var recorded = db.MitigationValidations
            .Where(v => v.IncidentId == incidentId && v.WorkflowId == wfGuid)
            .OrderByDescending(v => v.ValidatedAt)
            .AsNoTracking()
            .FirstOrDefault();

        if (recorded is not null)
        {
            var checks = Deserialize<List<ValidationCheckJson>>(recorded.ChecksJson)
                ?.Select(c => new GetMitigationValidation.ValidationCheckDto(c.CheckName, null, c.IsPassed, c.ObservedValue))
                .ToArray()
                ?? [];

            return new GetMitigationValidation.Response(
                wfGuid, recorded.Status, checks,
                recorded.ObservedOutcome, null,
                recorded.ValidatedAt, recorded.ValidatedBy);
        }

        // Fall back to seed validation data stored in workflow's JSON
        var workflow = db.MitigationWorkflows
            .AsNoTracking()
            .SingleOrDefault(w => w.Id == MitigationWorkflowRecordId.From(wfGuid)
                && w.IncidentId == incidentId);

        if (workflow is null)
            return null;

        return null;
    }

    public RecordMitigationValidation.Response? RecordMitigationValidation(
        string incidentId, string workflowId, ValidationStatus status,
        string? observedOutcome, string? validatedBy,
        IReadOnlyList<RecordMitigationValidation.ValidationCheckInput>? checks)
    {
        var wfGuid = Guid.TryParse(workflowId, out var parsed) ? parsed : Guid.NewGuid();
        var validatedAt = DateTimeOffset.UtcNow;

        string? checksJson = null;
        if (checks is { Count: > 0 })
        {
            var jsonChecks = checks.Select(c => new ValidationCheckJson
            {
                CheckName = c.CheckName,
                IsPassed = c.IsPassed,
                ObservedValue = c.ObservedValue,
            }).ToList();
            checksJson = Serialize(jsonChecks);
        }

        var validation = MitigationValidationLog.Create(
            MitigationValidationLogId.New(),
            incidentId, wfGuid, status,
            observedOutcome, validatedBy, validatedAt, checksJson);

        db.MitigationValidations.Add(validation);
        db.SaveChanges();

        return new RecordMitigationValidation.Response(wfGuid, status, validatedAt);
    }

    // ── Runbooks ─────────────────────────────────────────────────────────

    public IReadOnlyList<ListRunbooks.RunbookSummaryDto> GetRunbooks()
    {
        return db.Runbooks
            .OrderByDescending(r => r.PublishedAt)
            .AsNoTracking()
            .ToList()
            .Select(r =>
            {
                var stepCount = Deserialize<List<RunbookStepJson>>(r.StepsJson)?.Count ?? 0;
                return new ListRunbooks.RunbookSummaryDto(
                    r.Id.Value, r.Title, r.Description,
                    r.LinkedService, r.LinkedIncidentType,
                    stepCount, r.PublishedAt);
            })
            .ToList();
    }

    public GetRunbookDetail.Response? GetRunbookDetail(string runbookId)
    {
        if (!Guid.TryParse(runbookId, out var guid))
            return null;

        var runbook = db.Runbooks
            .AsNoTracking()
            .SingleOrDefault(r => r.Id == RunbookRecordId.From(guid));

        if (runbook is null)
            return null;

        var steps = Deserialize<List<RunbookStepJson>>(runbook.StepsJson)
            ?.Select(s => new GetRunbookDetail.RunbookStepDto(s.StepOrder, s.Title, s.Description, s.IsOptional))
            .ToArray()
            ?? [];

        var prerequisites = Deserialize<List<string>>(runbook.PrerequisitesJson) ?? [];

        return new GetRunbookDetail.Response(
            runbook.Id.Value, runbook.Title, runbook.Description,
            runbook.LinkedService, runbook.LinkedIncidentType,
            steps, prerequisites,
            runbook.PostNotes,
            runbook.MaintainedBy, runbook.PublishedAt, runbook.LastReviewedAt);
    }

    // ── JSON helpers ─────────────────────────────────────────────────────

    private static T? Deserialize<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, JsonOptions);

    // ── Internal JSON DTOs ───────────────────────────────────────────────

    internal sealed class LinkedServiceJson
    {
        public string ServiceId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string Criticality { get; set; } = string.Empty;
    }

    internal sealed class TimelineEntryJson
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    internal sealed class CorrelatedChangeJson
    {
        public Guid ChangeId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ChangeType { get; set; } = string.Empty;
        public string ConfidenceStatus { get; set; } = string.Empty;
        public DateTimeOffset DeployedAt { get; set; }
    }

    internal sealed class CorrelatedServiceJson
    {
        public string ServiceId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ImpactDescription { get; set; } = string.Empty;
    }

    internal sealed class CorrelatedDependencyJson
    {
        public string ServiceId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
    }

    internal sealed class ImpactedContractJson
    {
        public Guid ContractVersionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;
    }

    internal sealed class EvidenceObservationJson
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    internal sealed class RelatedContractJson
    {
        public Guid ContractVersionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;
        public string LifecycleState { get; set; } = string.Empty;
    }

    internal sealed class RunbookLinkJson
    {
        public string Title { get; set; } = string.Empty;
        public string? Url { get; set; }
    }

    internal sealed class MitigationActionJson
    {
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool Completed { get; set; }
    }

    internal sealed class MitigationRunbookJson
    {
        public string Title { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Description { get; set; }
    }

    internal sealed class MitigationRecommendationJson
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

    internal sealed class WorkflowStepJson
    {
        public int StepOrder { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsCompleted { get; set; }
        public string? CompletedBy { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public string? Notes { get; set; }
    }

    internal sealed class WorkflowDecisionJson
    {
        public MitigationDecisionType DecisionType { get; set; }
        public string DecidedBy { get; set; } = string.Empty;
        public DateTimeOffset DecidedAt { get; set; }
        public string? Reason { get; set; }
    }

    internal sealed class ValidationCheckJson
    {
        public string CheckName { get; set; } = string.Empty;
        public bool IsPassed { get; set; }
        public string? ObservedValue { get; set; }
    }

    internal sealed class RunbookStepJson
    {
        public int StepOrder { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsOptional { get; set; }
    }
}
