using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.AuditCompliance.Domain.Enums;

namespace NexTraceOne.AuditCompliance.Application.Abstractions;

/// <summary>
/// Repositório de resultados de compliance do módulo Audit.
/// </summary>
public interface IComplianceResultRepository
{
    /// <summary>Obtém um resultado pelo identificador.</summary>
    Task<ComplianceResult?> GetByIdAsync(ComplianceResultId id, CancellationToken cancellationToken);

    /// <summary>Lista resultados por política.</summary>
    Task<IReadOnlyList<ComplianceResult>> ListByPolicyIdAsync(CompliancePolicyId policyId, CancellationToken cancellationToken);

    /// <summary>Lista resultados por campanha.</summary>
    Task<IReadOnlyList<ComplianceResult>> ListByCampaignIdAsync(AuditCampaignId campaignId, CancellationToken cancellationToken);

    /// <summary>Lista resultados com filtros opcionais.</summary>
    Task<IReadOnlyList<ComplianceResult>> ListAsync(CompliancePolicyId? policyId, AuditCampaignId? campaignId, ComplianceOutcome? outcome, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo resultado.</summary>
    void Add(ComplianceResult result);
}
