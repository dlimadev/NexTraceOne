namespace NexTraceOne.AuditCompliance.Domain.Enums;

/// <summary>Nível de severidade de uma política de compliance.</summary>
public enum ComplianceSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>Estado de uma campanha de auditoria.</summary>
public enum CampaignStatus
{
    Planned,
    InProgress,
    Completed,
    Cancelled
}

/// <summary>Resultado da avaliação de compliance de um recurso.</summary>
public enum ComplianceOutcome
{
    Compliant,
    NonCompliant,
    PartiallyCompliant,
    NotApplicable
}
