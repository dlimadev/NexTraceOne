using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.AuditCompliance.Domain.Enums;

namespace NexTraceOne.AuditCompliance.Application.Abstractions;

/// <summary>
/// Repositório de campanhas de auditoria do módulo Audit.
/// </summary>
public interface IAuditCampaignRepository
{
    /// <summary>Obtém uma campanha pelo identificador.</summary>
    Task<AuditCampaign?> GetByIdAsync(AuditCampaignId id, CancellationToken cancellationToken);

    /// <summary>Lista campanhas com filtro opcional de status.</summary>
    Task<IReadOnlyList<AuditCampaign>> ListAsync(CampaignStatus? status, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova campanha.</summary>
    void Add(AuditCampaign campaign);

    /// <summary>Atualiza uma campanha existente.</summary>
    void Update(AuditCampaign campaign);
}
