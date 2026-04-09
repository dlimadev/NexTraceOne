using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>
/// Repositório para gates de promoção configuráveis.
/// </summary>
public interface IPromotionGateRepository
{
    /// <summary>Busca um gate de promoção pelo identificador.</summary>
    Task<PromotionGate?> GetByIdAsync(PromotionGateId id, CancellationToken cancellationToken = default);

    /// <summary>Lista gates de promoção por par de ambientes (origem → destino).</summary>
    Task<IReadOnlyList<PromotionGate>> ListByEnvironmentAsync(string environmentFrom, string environmentTo, CancellationToken cancellationToken = default);

    /// <summary>Lista todos os gates de promoção ativos.</summary>
    Task<IReadOnlyList<PromotionGate>> ListActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo gate de promoção.</summary>
    Task AddAsync(PromotionGate gate, CancellationToken cancellationToken = default);
}
