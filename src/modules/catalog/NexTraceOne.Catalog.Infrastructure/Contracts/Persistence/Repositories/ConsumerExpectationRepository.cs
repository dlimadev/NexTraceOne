using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de expectativas de consumidores para Consumer-Driven Contract Testing.
/// </summary>
internal sealed class ConsumerExpectationRepository(ContractsDbContext context)
    : IConsumerExpectationRepository
{
    public void Add(ConsumerExpectation expectation) => context.ConsumerExpectations.Add(expectation);

    public async Task<ConsumerExpectation?> GetByIdAsync(ConsumerExpectationId id, CancellationToken ct = default)
        => await context.ConsumerExpectations.SingleOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<ConsumerExpectation>> ListByApiAssetAsync(Guid apiAssetId, CancellationToken ct = default)
        => await context.ConsumerExpectations
            .Where(e => e.ApiAssetId == apiAssetId && e.IsActive)
            .OrderBy(e => e.ConsumerServiceName)
            .ToListAsync(ct);

    public async Task<ConsumerExpectation?> GetByApiAssetAndConsumerAsync(
        Guid apiAssetId, string consumerServiceName, CancellationToken ct = default)
        => await context.ConsumerExpectations
            .SingleOrDefaultAsync(e => e.ApiAssetId == apiAssetId
                && e.ConsumerServiceName == consumerServiceName, ct);
}
