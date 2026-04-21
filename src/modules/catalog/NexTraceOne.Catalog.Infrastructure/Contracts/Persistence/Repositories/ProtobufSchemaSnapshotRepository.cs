using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para snapshots analisados de schemas Protobuf.
/// Wave H.1 — Protobuf Schema Analysis.
/// </summary>
internal sealed class ProtobufSchemaSnapshotRepository(ContractsDbContext context)
    : IProtobufSchemaSnapshotRepository
{
    public void Add(ProtobufSchemaSnapshot snapshot)
        => context.ProtobufSchemaSnapshots.Add(snapshot);

    public async Task<ProtobufSchemaSnapshot?> GetByIdAsync(
        ProtobufSchemaSnapshotId id,
        CancellationToken cancellationToken = default)
        => await context.ProtobufSchemaSnapshots
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<ProtobufSchemaSnapshot?> GetLatestByApiAssetAsync(
        Guid apiAssetId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
        => await context.ProtobufSchemaSnapshots
            .Where(s => s.ApiAssetId == apiAssetId && s.TenantId == tenantId)
            .OrderByDescending(s => s.CapturedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ProtobufSchemaSnapshot>> ListByApiAssetAsync(
        Guid apiAssetId,
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await context.ProtobufSchemaSnapshots
            .Where(s => s.ApiAssetId == apiAssetId && s.TenantId == tenantId)
            .OrderByDescending(s => s.CapturedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
}
