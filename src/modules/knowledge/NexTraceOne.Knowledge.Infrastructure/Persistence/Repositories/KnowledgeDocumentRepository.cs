using Microsoft.EntityFrameworkCore;

using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para KnowledgeDocument.
/// </summary>
internal sealed class KnowledgeDocumentRepository(KnowledgeDbContext context) : IKnowledgeDocumentRepository
{
    public async Task<KnowledgeDocument?> GetByIdAsync(KnowledgeDocumentId id, CancellationToken cancellationToken = default)
        => await context.KnowledgeDocuments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<KnowledgeDocument?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => await context.KnowledgeDocuments.FirstOrDefaultAsync(d => d.Slug == slug, cancellationToken);

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

    public async Task<(IReadOnlyList<KnowledgeDocument> Items, int TotalCount)> ListAsync(
        DocumentCategory? category,
        DocumentStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.KnowledgeDocuments.AsQueryable();

        if (category.HasValue)
            query = query.Where(d => d.Category == category.Value);

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
