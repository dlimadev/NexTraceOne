using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de comentários de negociações de contratos.
/// Persiste e consulta comentários para rastreabilidade da revisão colaborativa.
/// </summary>
internal sealed class NegotiationCommentRepository(ContractsDbContext context)
    : INegotiationCommentRepository
{
    /// <inheritdoc />
    public async Task<NegotiationComment?> GetByIdAsync(NegotiationCommentId id, CancellationToken cancellationToken)
        => await context.NegotiationComments
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<NegotiationComment>> ListByNegotiationIdAsync(Guid negotiationId, CancellationToken cancellationToken)
        => await context.NegotiationComments
            .AsNoTracking()
            .Where(x => x.NegotiationId == negotiationId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(NegotiationComment comment, CancellationToken cancellationToken)
        => await context.NegotiationComments.AddAsync(comment, cancellationToken);
}
