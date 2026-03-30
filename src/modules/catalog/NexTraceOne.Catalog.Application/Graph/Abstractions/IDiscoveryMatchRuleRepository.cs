using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Application.Graph.Abstractions;

/// <summary>
/// Repositório de regras de matching automático para service discovery.
/// </summary>
public interface IDiscoveryMatchRuleRepository
{
    /// <summary>Obtém uma regra pelo identificador.</summary>
    Task<DiscoveryMatchRule?> GetByIdAsync(DiscoveryMatchRuleId id, CancellationToken cancellationToken);

    /// <summary>Lista todas as regras ativas ordenadas por prioridade.</summary>
    Task<IReadOnlyList<DiscoveryMatchRule>> ListActiveAsync(CancellationToken cancellationToken);

    /// <summary>Lista todas as regras (ativas e inativas).</summary>
    Task<IReadOnlyList<DiscoveryMatchRule>> ListAllAsync(CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova regra.</summary>
    void Add(DiscoveryMatchRule rule);

    /// <summary>Remove uma regra.</summary>
    void Remove(DiscoveryMatchRule rule);
}
