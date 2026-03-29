using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Repositories;

internal sealed class ImsTransactionRepository(LegacyAssetsDbContext context)
    : RepositoryBase<ImsTransaction, ImsTransactionId>(context), IImsTransactionRepository
{
    private readonly LegacyAssetsDbContext _context = context;

    public override async Task<ImsTransaction?> GetByIdAsync(ImsTransactionId id, CancellationToken ct = default)
        => await _context.ImsTransactions.SingleOrDefaultAsync(t => t.Id == id, ct);

    public async Task<ImsTransaction?> GetByCodeAndSystemAsync(string transactionCode, MainframeSystemId systemId, CancellationToken cancellationToken)
        => await _context.ImsTransactions
            .SingleOrDefaultAsync(t => t.TransactionCode == transactionCode && t.SystemId == systemId, cancellationToken);

    public async Task<IReadOnlyList<ImsTransaction>> ListBySystemAsync(MainframeSystemId systemId, CancellationToken cancellationToken)
        => await _context.ImsTransactions
            .Where(t => t.SystemId == systemId)
            .OrderBy(t => t.TransactionCode)
            .ToListAsync(cancellationToken);
}
