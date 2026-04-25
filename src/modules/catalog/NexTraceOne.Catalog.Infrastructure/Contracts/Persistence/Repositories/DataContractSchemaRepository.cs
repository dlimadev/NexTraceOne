using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para schemas de Data Contracts. CC-03.
/// </summary>
internal sealed class DataContractSchemaRepository(ContractsDbContext context) : IDataContractSchemaRepository
{
    public async Task AddAsync(DataContractSchema schema, CancellationToken ct)
        => await context.DataContractSchemas.AddAsync(schema, ct);

    public async Task<DataContractSchema?> GetLatestByApiAssetAsync(
        Guid apiAssetId, string tenantId, CancellationToken ct)
        => await context.DataContractSchemas
            .Where(s => s.ApiAssetId == apiAssetId && s.TenantId == tenantId)
            .OrderByDescending(s => s.CapturedAt)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

    public async Task<(IReadOnlyList<DataContractSchema> Items, int TotalCount)> ListByApiAssetAsync(
        Guid apiAssetId, string tenantId, int page, int pageSize, CancellationToken ct)
    {
        var query = context.DataContractSchemas
            .Where(s => s.ApiAssetId == apiAssetId && s.TenantId == tenantId);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.CapturedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
