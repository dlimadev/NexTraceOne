using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de resultados de diff semântico assistido por IA entre versões de contrato.
/// Persiste e consulta análises semânticas com classificação, consumidores afetados e mitigação.
/// </summary>
internal sealed class SemanticDiffResultRepository(ContractsDbContext context)
    : ISemanticDiffResultRepository
{
    /// <inheritdoc />
    public async Task<SemanticDiffResult?> GetByIdAsync(SemanticDiffResultId id, CancellationToken cancellationToken)
        => await context.SemanticDiffResults
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<SemanticDiffResult?> GetByVersionPairAsync(string contractVersionFromId, string contractVersionToId, CancellationToken cancellationToken)
        => await context.SemanticDiffResults
            .Where(x => x.ContractVersionFromId == contractVersionFromId && x.ContractVersionToId == contractVersionToId)
            .OrderByDescending(x => x.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SemanticDiffResult>> ListByContractVersionAsync(string contractVersionId, CancellationToken cancellationToken)
        => await context.SemanticDiffResults
            .AsNoTracking()
            .Where(x => x.ContractVersionFromId == contractVersionId || x.ContractVersionToId == contractVersionId)
            .OrderByDescending(x => x.GeneratedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(SemanticDiffResult result, CancellationToken cancellationToken)
        => await context.SemanticDiffResults.AddAsync(result, cancellationToken);
}
