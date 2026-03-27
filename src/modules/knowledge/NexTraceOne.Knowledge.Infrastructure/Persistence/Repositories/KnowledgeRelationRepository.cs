using Microsoft.EntityFrameworkCore;

using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para KnowledgeRelation.
/// </summary>
internal sealed class KnowledgeRelationRepository(KnowledgeDbContext context) : IKnowledgeRelationRepository
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
