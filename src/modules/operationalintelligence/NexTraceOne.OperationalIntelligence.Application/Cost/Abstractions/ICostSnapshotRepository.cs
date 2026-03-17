using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

/// <summary>
/// Contrato de repositório para a entidade CostSnapshot.
/// Provê operações de leitura e escrita para snapshots de custo de infraestrutura.
/// </summary>
public interface ICostSnapshotRepository
{
    /// <summary>Busca um snapshot de custo pelo seu identificador.</summary>
    Task<CostSnapshot?> GetByIdAsync(CostSnapshotId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista snapshots de custo de um serviço e ambiente, ordenados por data de captura descendente.
    /// Suporta paginação via page e pageSize.
    /// </summary>
    Task<IReadOnlyList<CostSnapshot>> ListByServiceAsync(string serviceName, string environment, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo snapshot de custo ao repositório.</summary>
    void Add(CostSnapshot snapshot);
}
