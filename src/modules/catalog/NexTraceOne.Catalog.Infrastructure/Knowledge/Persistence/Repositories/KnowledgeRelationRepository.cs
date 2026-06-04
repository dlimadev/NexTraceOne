using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Knowledge.Abstractions;
using NexTraceOne.Catalog.Domain.Knowledge.Entities;
using NexTraceOne.Catalog.Domain.Knowledge.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Knowledge.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para KnowledgeRelation.
/// </summary>
internal sealed class KnowledgeRelationRepository(ServiceCatalogDbContext context) : IKnowledgeRelationRepository
{
    public async Task<KnowledgeRelation?> GetByIdAsync(KnowledgeRelationId id, CancellationToken cancellationToken = default)
        => await context.KnowledgeRelations.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task AddAsync(KnowledgeRelation relation, CancellationToken cancellationToken = default)
        => await context.KnowledgeRelations.AddAsync(relation, cancellationToken);

    public void Remove(KnowledgeRelation relation)
        => context.KnowledgeRelations.Remove(relation);

    public async Task<IReadOnlyList<KnowledgeRelation>> ListBySourceAsync(Guid sourceEntityId, CancellationToken cancellationToken = default)
        => await context.KnowledgeRelations
            .Where(r => r.SourceEntityId == sourceEntityId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<KnowledgeRelation>> ListByTargetAsync(
        RelationType targetType,
        Guid targetEntityId,
        CancellationToken cancellationToken = default)
        => await context.KnowledgeRelations
            .Where(r => r.TargetType == targetType && r.TargetEntityId == targetEntityId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
}
