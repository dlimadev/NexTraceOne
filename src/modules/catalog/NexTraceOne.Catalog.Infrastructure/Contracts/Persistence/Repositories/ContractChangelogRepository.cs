using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de ContractChangelog usando EF Core.
/// </summary>
internal sealed class ContractChangelogRepository(ContractsDbContext context)
    : IContractChangelogRepository
{
    public async Task<ContractChangelog?> GetByIdAsync(
        ContractChangelogId id, CancellationToken cancellationToken)
        => await context.ContractChangelogs.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ContractChangelog>> ListByApiAssetAsync(
        string apiAssetId, CancellationToken cancellationToken)
        => await context.ContractChangelogs
            .Where(c => c.ApiAssetId == apiAssetId)
            .OrderByDescending(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ContractChangelog>> ListPendingApprovalAsync(
        CancellationToken cancellationToken)
        => await context.ContractChangelogs
            .Where(c => !c.IsApproved)
            .OrderBy(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ContractChangelog changelog, CancellationToken cancellationToken)
        => await context.ContractChangelogs.AddAsync(changelog, cancellationToken);

    public async Task<IReadOnlyList<ContractChangelog>> ListByTenantInPeriodAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
        => await context.ContractChangelogs
            .Where(c => c.TenantId == tenantId
                        && c.CreatedAt >= from
                        && c.CreatedAt <= to)
            .OrderByDescending(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
}
