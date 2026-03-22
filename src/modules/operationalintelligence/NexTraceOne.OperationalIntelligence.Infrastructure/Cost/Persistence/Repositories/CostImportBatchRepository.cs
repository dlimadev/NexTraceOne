using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Repositories;

/// <summary>
/// Repositório de batches de importação de custo, implementando consultas específicas de negócio.
/// Isolamento total: acessa apenas CostIntelligenceDbContext — sem acesso cross-module.
/// </summary>
internal sealed class CostImportBatchRepository(CostIntelligenceDbContext context)
    : RepositoryBase<CostImportBatch, CostImportBatchId>(context), ICostImportBatchRepository
{
    /// <summary>Busca um batch de importação pelo seu identificador.</summary>
    public override async Task<CostImportBatch?> GetByIdAsync(CostImportBatchId id, CancellationToken ct = default)
        => await context.CostImportBatches
            .SingleOrDefaultAsync(b => b.Id == id, ct);

    /// <summary>Verifica se já existe um batch para a mesma fonte e período.</summary>
    public async Task<bool> ExistsBySourceAndPeriodAsync(string source, string period, CancellationToken cancellationToken = default)
        => await context.CostImportBatches
            .AnyAsync(b => b.Source == source && b.Period == period, cancellationToken);
}
