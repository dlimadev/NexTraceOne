using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Alerta proactivo emitido pelo Proactive Architecture Guardian.
/// Detecta padrões de risco antes de incidentes ocorrerem.
/// </summary>
public sealed class GuardianAlert : AuditableEntity<GuardianAlertId>
{
    private GuardianAlert() { }

    public string ServiceName { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string PatternDetected { get; private set; } = string.Empty;
    public string Recommendation { get; private set; } = string.Empty;
    public double Confidence { get; private set; }
    public string Severity { get; private set; } = string.Empty;
    public string Status { get; private set; } = "open";
    public string? AcknowledgedBy { get; private set; }
    public DateTimeOffset? AcknowledgedAt { get; private set; }
    public string? DismissReason { get; private set; }
    public bool WasActualIssue { get; private set; }
    public Guid TenantId { get; private set; }
    public DateTimeOffset DetectedAt { get; private set; }
    public uint RowVersion { get; set; }

    public static GuardianAlert Emit(
        string serviceName,
        string category,
        string patternDetected,
        string recommendation,
        double confidence,
        string severity,
        Guid tenantId,
        DateTimeOffset detectedAt)
    {
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(patternDetected);

        return new GuardianAlert
        {
            Id = GuardianAlertId.New(),
            ServiceName = serviceName,
            Category = category ?? string.Empty,
            PatternDetected = patternDetected,
            Recommendation = recommendation ?? string.Empty,
            Confidence = confidence,
            Severity = severity ?? string.Empty,
            TenantId = tenantId,
            DetectedAt = detectedAt,
            Status = "open",
        };
    }

    public void Acknowledge(string userId, DateTimeOffset acknowledgedAt)
    {
        Guard.Against.NullOrWhiteSpace(userId);
        Status = "acknowledged";
        AcknowledgedBy = userId;
        AcknowledgedAt = acknowledgedAt;
    }

    public void Resolve(bool wasActualIssue)
    {
        Status = "resolved";
        WasActualIssue = wasActualIssue;
    }

    public void Dismiss(string reason)
    {
        Status = "dismissed";
        DismissReason = reason;
    }
}

/// <summary>Identificador fortemente tipado de GuardianAlert.</summary>
public sealed record GuardianAlertId(Guid Value) : TypedIdBase(Value)
{
    public static GuardianAlertId New() => new(Guid.NewGuid());
    public static GuardianAlertId From(Guid id) => new(id);
}
