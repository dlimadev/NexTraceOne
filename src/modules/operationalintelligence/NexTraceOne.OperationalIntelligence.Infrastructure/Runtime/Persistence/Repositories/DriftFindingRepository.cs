using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories;

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

    /// <summary>Lista todos os drift findings no tenant num período temporal (para relatórios de anomalias).</summary>
    public async Task<IReadOnlyList<DriftFinding>> ListByTenantInPeriodAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
        => await context.DriftFindings
            .Where(f => f.DetectedAt >= from && f.DetectedAt <= to)
            .OrderByDescending(f => f.DetectedAt)
            .ToListAsync(cancellationToken);
}
