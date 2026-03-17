using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;

/// <summary>
/// Contrato de repositório para solicitações de promoção.
/// </summary>
public interface IPromotionRequestRepository
{
    /// <summary>Busca uma solicitação de promoção pelo identificador.</summary>
    Task<PromotionRequest?> GetByIdAsync(PromotionRequestId id, CancellationToken ct);

    /// <summary>Adiciona uma nova solicitação de promoção ao contexto.</summary>
    void Add(PromotionRequest request);

    /// <summary>Atualiza uma solicitação de promoção existente no contexto.</summary>
    void Update(PromotionRequest request);

    /// <summary>Lista solicitações de promoção pelo status.</summary>
    Task<IReadOnlyList<PromotionRequest>> ListByStatusAsync(PromotionStatus status, CancellationToken ct);

    /// <summary>Lista solicitações de promoção pelo identificador da release.</summary>
    Task<IReadOnlyList<PromotionRequest>> ListByReleaseIdAsync(Guid releaseId, CancellationToken ct);

    /// <summary>Conta solicitações de promoção por status.</summary>
    Task<int> CountByStatusAsync(PromotionStatus status, CancellationToken ct);
}
