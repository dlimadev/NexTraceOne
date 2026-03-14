using NexTraceOne.CostIntelligence.Domain.Entities;

namespace NexTraceOne.CostIntelligence.Application.Abstractions;

/// <summary>
/// Contrato de repositório para a entidade CostAttribution.
/// Provê operações de leitura e escrita para atribuições de custo a APIs/serviços.
/// </summary>
public interface ICostAttributionRepository
{
    /// <summary>Busca uma atribuição de custo pelo seu identificador.</summary>
    Task<CostAttribution?> GetByIdAsync(CostAttributionId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista atribuições de custo de um serviço e ambiente, ordenadas por período descendente.
    /// Suporta paginação via page e pageSize.
    /// </summary>
    Task<IReadOnlyList<CostAttribution>> ListByServiceAsync(string serviceName, string environment, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista atribuições de custo dentro de um período específico, independentemente do serviço.
    /// Útil para relatórios consolidados de custo por período.
    /// </summary>
    Task<IReadOnlyList<CostAttribution>> ListByPeriodAsync(DateTimeOffset periodStart, DateTimeOffset periodEnd, CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma nova atribuição de custo ao repositório.</summary>
    void Add(CostAttribution attribution);
}
