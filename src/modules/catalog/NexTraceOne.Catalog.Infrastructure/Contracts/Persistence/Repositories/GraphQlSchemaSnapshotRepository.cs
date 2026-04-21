using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para snapshots analisados de schemas GraphQL.
/// Wave G.3 — GraphQL Schema Analysis.
/// </summary>
internal sealed class GraphQlSchemaSnapshotRepository(ContractsDbContext context)
    : IGraphQlSchemaSnapshotRepository
{
    public void Add(GraphQlSchemaSnapshot snapshot)
        => context.GraphQlSchemaSnapshots.Add(snapshot);

    public async Task<GraphQlSchemaSnapshot?> GetByIdAsync(
        GraphQlSchemaSnapshotId id,
        CancellationToken cancellationToken = default)
        => await context.GraphQlSchemaSnapshots
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<GraphQlSchemaSnapshot?> GetLatestByApiAssetAsync(
        Guid apiAssetId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
        => await context.GraphQlSchemaSnapshots
            .Where(s => s.ApiAssetId == apiAssetId && s.TenantId == tenantId)
            .OrderByDescending(s => s.CapturedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<GraphQlSchemaSnapshot>> ListByApiAssetAsync(
        Guid apiAssetId,
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await context.GraphQlSchemaSnapshots
            .Where(s => s.ApiAssetId == apiAssetId && s.TenantId == tenantId)
            .OrderByDescending(s => s.CapturedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
}
