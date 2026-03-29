using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;

/// <summary>
/// Repositório de transações IMS do catálogo legacy.
/// </summary>
public interface IImsTransactionRepository
{
    Task<ImsTransaction?> GetByIdAsync(ImsTransactionId id, CancellationToken cancellationToken);
    Task<ImsTransaction?> GetByCodeAndSystemAsync(string transactionCode, MainframeSystemId systemId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ImsTransaction>> ListBySystemAsync(MainframeSystemId systemId, CancellationToken cancellationToken);
    void Add(ImsTransaction transaction);
}
