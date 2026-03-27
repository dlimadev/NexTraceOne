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

    public async Task<IReadOnlyList<KnowledgeDocument>> SearchAsync(string searchTerm, int maxResults, CancellationToken cancellationToken = default)
    {
        var pattern = $"%{searchTerm}%";
        return await context.KnowledgeDocuments
            .Where(d =>
                EF.Functions.ILike(d.Title, pattern) ||
                EF.Functions.ILike(d.Content, pattern) ||
                (d.Summary != null && EF.Functions.ILike(d.Summary, pattern)))
            .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }
}
