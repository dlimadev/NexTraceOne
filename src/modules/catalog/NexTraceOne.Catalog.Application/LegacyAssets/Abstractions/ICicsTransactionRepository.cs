using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;

/// <summary>
/// Repositório de transações CICS do catálogo legacy.
/// </summary>
public interface ICicsTransactionRepository
{
    Task<CicsTransaction?> GetByIdAsync(CicsTransactionId id, CancellationToken cancellationToken);
    Task<CicsTransaction?> GetByTransactionIdAndSystemAsync(string transactionId, MainframeSystemId systemId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CicsTransaction>> ListBySystemAsync(MainframeSystemId systemId, CancellationToken cancellationToken);
    void Add(CicsTransaction transaction);
}
