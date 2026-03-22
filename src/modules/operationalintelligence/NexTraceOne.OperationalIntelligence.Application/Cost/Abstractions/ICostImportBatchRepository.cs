using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

/// <summary>
/// Contrato de repositório para a entidade CostImportBatch.
/// Provê operações de leitura e escrita para batches de importação de custo.
/// </summary>
public interface ICostImportBatchRepository
{
    /// <summary>Busca um batch de importação pelo seu identificador.</summary>
    Task<CostImportBatch?> GetByIdAsync(CostImportBatchId id, CancellationToken cancellationToken = default);

    /// <summary>Verifica se já existe um batch para a mesma fonte e período.</summary>
    Task<bool> ExistsBySourceAndPeriodAsync(string source, string period, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo batch de importação ao repositório.</summary>
    void Add(CostImportBatch batch);
}
