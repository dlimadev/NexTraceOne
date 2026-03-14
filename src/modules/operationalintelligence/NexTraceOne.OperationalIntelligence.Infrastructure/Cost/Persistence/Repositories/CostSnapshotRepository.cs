using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.CostIntelligence.Application.Abstractions;
using NexTraceOne.CostIntelligence.Domain.Entities;

namespace NexTraceOne.CostIntelligence.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de snapshots de custo, implementando consultas específicas de negócio.
/// Isolamento total: acessa apenas CostIntelligenceDbContext — sem acesso cross-module.
/// </summary>
internal sealed class CostSnapshotRepository(CostIntelligenceDbContext context)
    : RepositoryBase<CostSnapshot, CostSnapshotId>(context), ICostSnapshotRepository
{
    /// <summary>Busca um snapshot de custo pelo seu identificador.</summary>
    public override async Task<CostSnapshot?> GetByIdAsync(CostSnapshotId id, CancellationToken ct = default)
        => await context.CostSnapshots
            .SingleOrDefaultAsync(s => s.Id == id, ct);

    /// <summary>Lista snapshots de custo de um serviço e ambiente, ordenados por data de captura descendente.</summary>
    public async Task<IReadOnlyList<CostSnapshot>> ListByServiceAsync(
        string serviceName,
        string environment,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await context.CostSnapshots
            .Where(s => s.ServiceName == serviceName && s.Environment == environment)
            .OrderByDescending(s => s.CapturedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
}
