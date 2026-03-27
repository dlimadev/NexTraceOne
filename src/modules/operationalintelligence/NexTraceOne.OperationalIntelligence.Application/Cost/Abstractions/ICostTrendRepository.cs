using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

/// <summary>
/// Contrato de repositório para a entidade CostTrend.
/// Provê operações de leitura e escrita para análises de tendência de custo persistidas.
/// </summary>
public interface ICostTrendRepository
{
    /// <summary>Busca uma análise de tendência de custo pelo seu identificador.</summary>
    Task<CostTrend?> GetByIdAsync(CostTrendId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista tendências de custo de um serviço e ambiente, ordenadas por data de início descendente.
    /// Suporta paginação via page e pageSize.
    /// </summary>
    Task<IReadOnlyList<CostTrend>> ListByServiceAsync(
        string serviceName,
        string environment,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma nova análise de tendência de custo ao repositório.</summary>
    void Add(CostTrend trend);
}
