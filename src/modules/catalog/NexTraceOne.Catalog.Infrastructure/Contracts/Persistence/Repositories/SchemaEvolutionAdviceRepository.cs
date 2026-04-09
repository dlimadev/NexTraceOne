using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de análises de evolução de schema de contratos.
/// Persiste e consulta análises de compatibilidade entre versões de API Assets.
/// </summary>
internal sealed class SchemaEvolutionAdviceRepository(ContractsDbContext context)
    : ISchemaEvolutionAdviceRepository
{
    /// <inheritdoc />
    public async Task<SchemaEvolutionAdvice?> GetByIdAsync(SchemaEvolutionAdviceId id, CancellationToken cancellationToken)
        => await context.SchemaEvolutionAdvices
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<SchemaEvolutionAdvice?> GetLatestByApiAssetAsync(Guid apiAssetId, CancellationToken cancellationToken)
        => await context.SchemaEvolutionAdvices
            .Where(x => x.ApiAssetId == apiAssetId)
            .OrderByDescending(x => x.AnalyzedAt)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SchemaEvolutionAdvice>> ListByApiAssetAsync(Guid? apiAssetId, CancellationToken cancellationToken)
    {
        var query = context.SchemaEvolutionAdvices.AsNoTracking();

        if (apiAssetId.HasValue)
            query = query.Where(x => x.ApiAssetId == apiAssetId.Value);

        return await query
            .OrderByDescending(x => x.AnalyzedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(SchemaEvolutionAdvice advice, CancellationToken cancellationToken)
        => await context.SchemaEvolutionAdvices.AddAsync(advice, cancellationToken);
}
