using Microsoft.EntityFrameworkCore;

using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para snapshots de métricas DORA utilizados nos benchmarks cross-tenant.
/// </summary>
internal sealed class BenchmarkSnapshotRepository(ChangeIntelligenceDbContext context) : IBenchmarkSnapshotRepository
{
    /// <summary>Obtém um snapshot pelo identificador.</summary>
    public async Task<BenchmarkSnapshotRecord?> GetByIdAsync(BenchmarkSnapshotRecordId id, CancellationToken ct = default)
        => await context.BenchmarkSnapshots
            .SingleOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);

    /// <summary>Lista snapshots de um tenant no período especificado.</summary>
    public async Task<IReadOnlyList<BenchmarkSnapshotRecord>> ListByTenantAsync(string tenantId, DateTimeOffset since, CancellationToken ct = default)
        => await context.BenchmarkSnapshots
            .Where(s => s.TenantId == tenantId && s.PeriodStart >= since && !s.IsDeleted)
            .OrderByDescending(s => s.PeriodStart)
            .ToListAsync(ct);

    /// <summary>
    /// Lista todos os snapshots anonimizados no período especificado.
    /// Usado exclusivamente para cálculos de percentil cross-tenant — dados de outros tenants nunca são expostos individualmente.
    /// </summary>
    public async Task<IReadOnlyList<BenchmarkSnapshotRecord>> ListAnonymizedAsync(DateTimeOffset since, CancellationToken ct = default)
        => await context.BenchmarkSnapshots
            .Where(s => s.IsAnonymizedForBenchmarks && s.PeriodStart >= since && !s.IsDeleted)
            .ToListAsync(ct);

    /// <summary>Adiciona um novo snapshot.</summary>
    public void Add(BenchmarkSnapshotRecord snapshot)
        => context.BenchmarkSnapshots.Add(snapshot);
}
