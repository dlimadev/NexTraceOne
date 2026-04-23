using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiGovernanceComplianceReport;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Reader de compliance de governança de IA — verifica se os modelos activos do tenant
/// cumprem os requisitos de governança: aprovação formal, audit trail, budget e políticas.
/// Por omissão satisfeita por <c>NullAiGovernanceComplianceReader</c> (honest-null).
/// Wave AT.3 — AI Model Quality &amp; Drift Governance.
/// </summary>
public interface IAiGovernanceComplianceReader
{
    /// <summary>
    /// Retorna dados de compliance por modelo activo no tenant.
    /// </summary>
    Task<IReadOnlyList<GetAiGovernanceComplianceReport.ModelComplianceRow>> GetComplianceRowsAsync(
        string tenantId,
        int auditTrailLookbackDays,
        int modelReviewDays,
        CancellationToken ct);

    /// <summary>
    /// Retorna chamadas que violaram políticas de acesso no período especificado.
    /// </summary>
    Task<IReadOnlyList<GetAiGovernanceComplianceReport.PolicyViolation>> GetPolicyViolationsAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);
}
