using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para inventário de consumidores reais de contratos. CC-04.
/// </summary>
internal sealed class ContractConsumerInventoryRepository(ContractsDbContext context)
    : IContractConsumerInventoryRepository
{
    public async Task<IReadOnlyList<ContractConsumerInventory>> ListByContractAsync(
        Guid contractId, string tenantId, CancellationToken ct)
        => await context.ContractConsumerInventories
            .Where(i => i.ContractId == contractId && i.TenantId == tenantId)
            .OrderByDescending(i => i.FrequencyPerDay)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<ContractConsumerInventory?> GetByUniqueKeyAsync(
        Guid contractId, string tenantId, string consumerService, string consumerEnvironment, CancellationToken ct)
        => await context.ContractConsumerInventories
            .SingleOrDefaultAsync(i =>
                i.ContractId == contractId &&
                i.TenantId == tenantId &&
                i.ConsumerService == consumerService &&
                i.ConsumerEnvironment == consumerEnvironment, ct);

    public async Task AddAsync(ContractConsumerInventory inventory, CancellationToken ct)
        => await context.ContractConsumerInventories.AddAsync(inventory, ct);

    public Task UpdateAsync(ContractConsumerInventory inventory, CancellationToken ct)
    {
        context.ContractConsumerInventories.Update(inventory);
        return Task.CompletedTask;
    }
}
