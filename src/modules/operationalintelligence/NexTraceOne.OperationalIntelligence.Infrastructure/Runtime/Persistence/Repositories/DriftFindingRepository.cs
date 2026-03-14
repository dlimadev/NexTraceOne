using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.RuntimeIntelligence.Application.Abstractions;
using NexTraceOne.RuntimeIntelligence.Domain.Entities;

namespace NexTraceOne.RuntimeIntelligence.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de findings de drift detectados entre baselines e snapshots.
/// Suporta listagem por severidade e estado de acknowledgment.
/// </summary>
internal sealed class DriftFindingRepository(RuntimeIntelligenceDbContext context)
    : RepositoryBase<DriftFinding, DriftFindingId>(context), IDriftFindingRepository
{
    /// <summary>Busca um drift finding pelo seu identificador.</summary>
    public override async Task<DriftFinding?> GetByIdAsync(DriftFindingId id, CancellationToken ct = default)
        => await context.DriftFindings
            .SingleOrDefaultAsync(f => f.Id == id, ct);

    /// <summary>Lista drift findings de um serviço e ambiente, ordenados por data de detecção descendente.</summary>
    public async Task<IReadOnlyList<DriftFinding>> ListByServiceAsync(
        string serviceName,
        string environment,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await context.DriftFindings
            .Where(f => f.ServiceName == serviceName && f.Environment == environment)
            .OrderByDescending(f => f.DetectedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    /// <summary>Lista drift findings não reconhecidos, ordenados por severidade e data.</summary>
    public async Task<IReadOnlyList<DriftFinding>> ListUnacknowledgedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await context.DriftFindings
            .Where(f => !f.IsAcknowledged)
            .OrderByDescending(f => f.Severity)
            .ThenByDescending(f => f.DetectedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
}
