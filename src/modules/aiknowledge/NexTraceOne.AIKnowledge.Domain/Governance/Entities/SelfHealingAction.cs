using System.Text.Json;
using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Acção de auto-remediação proposta ou executada pelo AI.
/// Suporta três modos: automatic, one_click e suggestion.
/// </summary>
public sealed class SelfHealingAction : AuditableEntity<SelfHealingActionId>
{
    private SelfHealingAction() { }

    public string IncidentId { get; private set; } = string.Empty;
    public string ServiceName { get; private set; } = string.Empty;
    public string ActionType { get; private set; } = string.Empty;
    public string ActionDescription { get; private set; } = string.Empty;
    public string Status { get; private set; } = "pending";
    public double Confidence { get; private set; }
    public string RiskLevel { get; private set; } = string.Empty;
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? ExecutedAt { get; private set; }
    public string? Result { get; private set; }
    public string AuditTrailJson { get; private set; } = "[]";
    public Guid TenantId { get; private set; }
    public DateTimeOffset ProposedAt { get; private set; }
    public uint RowVersion { get; set; }

    public static SelfHealingAction Propose(
        string incidentId,
        string serviceName,
        string actionType,
        string actionDescription,
        double confidence,
        string riskLevel,
        Guid tenantId,
        DateTimeOffset proposedAt)
    {
        Guard.Against.NullOrWhiteSpace(incidentId);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(actionType);
        Guard.Against.NullOrWhiteSpace(actionDescription);
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));

        return new SelfHealingAction
        {
            Id = SelfHealingActionId.New(),
            IncidentId = incidentId,
            ServiceName = serviceName,
            ActionType = actionType,
            ActionDescription = actionDescription,
            Confidence = confidence,
            RiskLevel = riskLevel,
            TenantId = tenantId,
            ProposedAt = proposedAt,
            Status = "pending",
        };
    }

    public void Approve(string approvedBy, DateTimeOffset approvedAt)
    {
        Guard.Against.NullOrWhiteSpace(approvedBy);
        ApprovedBy = approvedBy;
        ApprovedAt = approvedAt;
        Status = "approved";
        AppendAuditEvent($"Approved by {approvedBy} at {approvedAt:u}");
    }

    public void MarkExecuting()
    {
        Status = "executing";
        AppendAuditEvent("Execution started");
    }

    public void MarkCompleted(string result, DateTimeOffset executedAt)
    {
        Status = "completed";
        Result = result;
        ExecutedAt = executedAt;
        AppendAuditEvent($"Completed at {executedAt:u}: {result}");
    }

    public void MarkFailed(string errorMessage)
    {
        Status = "failed";
        Result = errorMessage;
        AppendAuditEvent($"Failed: {errorMessage}");
    }

    private void AppendAuditEvent(string message)
    {
        var list = JsonSerializer.Deserialize<List<string>>(AuditTrailJson) ?? [];
        list.Add($"[{DateTimeOffset.UtcNow:u}] {message}");
        AuditTrailJson = JsonSerializer.Serialize(list);
    }
}

/// <summary>Identificador fortemente tipado de SelfHealingAction.</summary>
public sealed record SelfHealingActionId(Guid Value) : TypedIdBase(Value)
{
    public static SelfHealingActionId New() => new(Guid.NewGuid());
    public static SelfHealingActionId From(Guid id) => new(id);
}
