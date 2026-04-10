using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de scores de saúde de contratos.
/// Persiste e consulta o score de saúde contínuo por API Asset.
/// </summary>
internal sealed class ContractHealthScoreRepository(ContractsDbContext context)
    : IContractHealthScoreRepository
{
    /// <inheritdoc />
    public async Task<ContractHealthScore?> GetByApiAssetIdAsync(Guid apiAssetId, CancellationToken cancellationToken)
        => await context.ContractHealthScores
            .SingleOrDefaultAsync(x => x.ApiAssetId == apiAssetId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractHealthScore>> ListBelowThresholdAsync(int threshold, CancellationToken cancellationToken)
        => await context.ContractHealthScores
            .AsNoTracking()
            .Where(x => x.OverallScore < threshold)
            .OrderBy(x => x.OverallScore)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(ContractHealthScore score, CancellationToken cancellationToken)
        => await context.ContractHealthScores.AddAsync(score, cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(ContractHealthScore score, CancellationToken cancellationToken)
    {
        context.ContractHealthScores.Update(score);
        return Task.CompletedTask;
    }
}
