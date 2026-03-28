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

        return await context.KnowledgeDocuments
            .Select(d => new
            {
                Document = d,
                SearchVector = EF.Functions.ToTsVector(
                    "simple",
                    (d.Title ?? string.Empty) + " " +
                    (d.Summary ?? string.Empty) + " " +
                    (d.Content ?? string.Empty))
            })
            .Where(x => x.SearchVector.Matches(EF.Functions.PlainToTsQuery("simple", term)))
            .OrderByDescending(x => x.SearchVector.Rank(EF.Functions.PlainToTsQuery("simple", term)))
            .Select(x => x.Document)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }
}
