using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.RuntimeIntelligence.Application.Abstractions;
using NexTraceOne.RuntimeIntelligence.Domain.Entities;

namespace NexTraceOne.RuntimeIntelligence.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de snapshots de runtime, implementando consultas de saúde de serviços.
/// Isolamento total: acessa apenas RuntimeIntelligenceDbContext — sem acesso cross-module.
/// </summary>
internal sealed class RuntimeSnapshotRepository(RuntimeIntelligenceDbContext context)
    : RepositoryBase<RuntimeSnapshot, RuntimeSnapshotId>(context), IRuntimeSnapshotRepository
{
    /// <summary>Busca um snapshot de runtime pelo seu identificador.</summary>
    public override async Task<RuntimeSnapshot?> GetByIdAsync(RuntimeSnapshotId id, CancellationToken ct = default)
        => await context.RuntimeSnapshots
            .SingleOrDefaultAsync(s => s.Id == id, ct);

    /// <summary>Lista snapshots de runtime de um serviço e ambiente, ordenados por data de captura descendente.</summary>
    public async Task<IReadOnlyList<RuntimeSnapshot>> ListByServiceAsync(
        string serviceName,
        string environment,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await context.RuntimeSnapshots
            .Where(s => s.ServiceName == serviceName && s.Environment == environment)
            .OrderByDescending(s => s.CapturedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    /// <summary>Obtém o snapshot mais recente de um serviço e ambiente.</summary>
    public async Task<RuntimeSnapshot?> GetLatestByServiceAsync(
        string serviceName,
        string environment,
        CancellationToken cancellationToken = default)
        => await context.RuntimeSnapshots
            .Where(s => s.ServiceName == serviceName && s.Environment == environment)
            .OrderByDescending(s => s.CapturedAt)
            .FirstOrDefaultAsync(cancellationToken);
}
