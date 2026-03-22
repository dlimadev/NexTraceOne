using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo Audit com códigos i18n.
/// </summary>
public static class AuditErrors
{
    /// <summary>Evento de auditoria não encontrado.</summary>
    public static Error EventNotFound(Guid eventId)
        => Error.NotFound("Audit.Event.NotFound", "Audit event '{0}' was not found.", eventId);

    /// <summary>Integridade da cadeia de hash violada.</summary>
    public static Error ChainIntegrityViolation(long sequenceNumber)
        => Error.Security("Audit.Chain.IntegrityViolation", "Chain integrity violated at sequence '{0}'.", sequenceNumber);

    /// <summary>Política de retenção não encontrada.</summary>
    public static Error RetentionPolicyNotFound(Guid policyId)
        => Error.NotFound("Audit.RetentionPolicy.NotFound", "Retention policy '{0}' was not found.", policyId);

    /// <summary>Política de compliance não encontrada.</summary>
    public static Error CompliancePolicyNotFound(Guid policyId)
        => Error.NotFound("Audit.CompliancePolicy.NotFound", "Compliance policy '{0}' was not found.", policyId);

    /// <summary>Campanha de auditoria não encontrada.</summary>
    public static Error CampaignNotFound(Guid campaignId)
        => Error.NotFound("Audit.Campaign.NotFound", "Audit campaign '{0}' was not found.", campaignId);
}
