using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Repositório de campanhas de recertificação de acessos.
/// </summary>
public interface IAccessReviewRepository
{
    /// <summary>Obtém uma campanha pelo identificador, incluindo seus itens.</summary>
    Task<AccessReviewCampaign?> GetByIdWithItemsAsync(AccessReviewCampaignId id, CancellationToken cancellationToken);

    /// <summary>Lista campanhas abertas no tenant.</summary>
    Task<IReadOnlyList<AccessReviewCampaign>> ListOpenByTenantAsync(TenantId tenantId, CancellationToken cancellationToken);

    /// <summary>Obtém um item de revisão pelo identificador.</summary>
    Task<AccessReviewItem?> GetItemByIdAsync(AccessReviewItemId id, CancellationToken cancellationToken);

    /// <summary>Lista itens pendentes atribuídos a um reviewer.</summary>
    Task<IReadOnlyList<AccessReviewItem>> ListPendingByReviewerAsync(UserId reviewerId, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova campanha para persistência.</summary>
    void Add(AccessReviewCampaign campaign);
}
