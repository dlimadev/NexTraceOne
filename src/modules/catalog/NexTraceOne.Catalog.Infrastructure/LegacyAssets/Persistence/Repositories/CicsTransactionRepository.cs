using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Repositories;

internal sealed class CicsTransactionRepository(LegacyAssetsDbContext context)
    : RepositoryBase<CicsTransaction, CicsTransactionId>(context), ICicsTransactionRepository
{
    private readonly LegacyAssetsDbContext _context = context;

    public override async Task<CicsTransaction?> GetByIdAsync(CicsTransactionId id, CancellationToken ct = default)
        => await _context.CicsTransactions.SingleOrDefaultAsync(t => t.Id == id, ct);

    public async Task<CicsTransaction?> GetByTransactionIdAndSystemAsync(string transactionId, MainframeSystemId systemId, CancellationToken cancellationToken)
        => await _context.CicsTransactions
            .SingleOrDefaultAsync(t => t.TransactionId == transactionId && t.SystemId == systemId, cancellationToken);

    public async Task<IReadOnlyList<CicsTransaction>> ListBySystemAsync(MainframeSystemId systemId, CancellationToken cancellationToken)
        => await _context.CicsTransactions
            .Where(t => t.SystemId == systemId)
            .OrderBy(t => t.TransactionId)
            .ToListAsync(cancellationToken);
}
