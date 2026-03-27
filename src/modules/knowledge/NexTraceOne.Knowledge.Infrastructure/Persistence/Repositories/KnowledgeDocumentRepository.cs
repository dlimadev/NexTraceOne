using Microsoft.EntityFrameworkCore;

using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Entities;

namespace NexTraceOne.Knowledge.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para KnowledgeDocument.
/// </summary>
internal sealed class KnowledgeDocumentRepository(KnowledgeDbContext context) : IKnowledgeDocumentRepository
{
    public async Task<KnowledgeDocument?> GetByIdAsync(KnowledgeDocumentId id, CancellationToken cancellationToken = default)
        => await context.KnowledgeDocuments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task AddAsync(KnowledgeDocument document, CancellationToken cancellationToken = default)
        => await context.KnowledgeDocuments.AddAsync(document, cancellationToken);

    public void Update(KnowledgeDocument document)
        => context.KnowledgeDocuments.Update(document);
}
