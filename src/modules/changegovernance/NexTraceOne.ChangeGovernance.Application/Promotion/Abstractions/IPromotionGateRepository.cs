using NexTraceOne.Promotion.Domain.Entities;

namespace NexTraceOne.Promotion.Application.Abstractions;

/// <summary>
/// Contrato de repositório para gates de promoção.
/// </summary>
public interface IPromotionGateRepository
{
    /// <summary>Busca um gate de promoção pelo identificador.</summary>
    Task<PromotionGate?> GetByIdAsync(PromotionGateId id, CancellationToken ct);

    /// <summary>Adiciona um novo gate de promoção ao contexto.</summary>
    void Add(PromotionGate gate);

    /// <summary>Atualiza um gate de promoção existente no contexto.</summary>
    void Update(PromotionGate gate);

    /// <summary>Lista todos os gates de promoção vinculados a um ambiente de deployment.</summary>
    Task<IReadOnlyList<PromotionGate>> ListByEnvironmentIdAsync(DeploymentEnvironmentId envId, CancellationToken ct);

    /// <summary>Lista apenas os gates obrigatórios vinculados a um ambiente de deployment.</summary>
    Task<IReadOnlyList<PromotionGate>> ListRequiredByEnvironmentIdAsync(DeploymentEnvironmentId envId, CancellationToken ct);
}
