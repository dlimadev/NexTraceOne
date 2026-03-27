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
        var term = searchTerm.Trim();
        if (term.Length == 0)
            return [];

        var tsQuery = EF.Functions.PlainToTsQuery("simple", term);

        return await context.KnowledgeDocuments
            .Where(d =>
                EF.Functions.ToTsVector(
                    "simple",
                    (d.Title ?? string.Empty) + " " +
                    (d.Summary ?? string.Empty) + " " +
                    (d.Content ?? string.Empty))
                .Matches(tsQuery))
            .OrderByDescending(d =>
                EF.Functions.ToTsVector(
                    "simple",
                    (d.Title ?? string.Empty) + " " +
                    (d.Summary ?? string.Empty) + " " +
                    (d.Content ?? string.Empty))
                .Rank(tsQuery))
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }
}
