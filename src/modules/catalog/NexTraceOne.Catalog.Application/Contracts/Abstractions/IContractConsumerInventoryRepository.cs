using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório para inventário de consumidores reais de contratos derivado de traces OTel.
/// CC-04.
/// </summary>
public interface IContractConsumerInventoryRepository
{
    /// <summary>Lista consumidores de um contrato específico.</summary>
    Task<IReadOnlyList<ContractConsumerInventory>> ListByContractAsync(
        Guid contractId, string tenantId, CancellationToken ct);

    /// <summary>Obtém um registo específico por contrato + consumidor + ambiente (para upsert).</summary>
    Task<ContractConsumerInventory?> GetByUniqueKeyAsync(
        Guid contractId, string tenantId, string consumerService, string consumerEnvironment, CancellationToken ct);

    /// <summary>Adiciona um novo registo.</summary>
    Task AddAsync(ContractConsumerInventory inventory, CancellationToken ct);

    /// <summary>Actualiza um registo existente.</summary>
    Task UpdateAsync(ContractConsumerInventory inventory, CancellationToken ct);
}
