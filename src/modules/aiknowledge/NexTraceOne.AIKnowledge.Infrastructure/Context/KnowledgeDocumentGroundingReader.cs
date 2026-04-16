using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.Knowledge.Infrastructure.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Context;

/// <summary>
/// Implementação do leitor de documentos do Knowledge Hub para grounding de IA.
/// Acesso somente-leitura ao KnowledgeDbContext.
/// </summary>
public sealed class KnowledgeDocumentGroundingReader(KnowledgeDbContext knowledgeDb) : IKnowledgeDocumentGroundingReader
{
    public async Task<IReadOnlyList<KnowledgeDocumentGroundingContext>> SearchDocumentsAsync(
        string searchTerm,
        int maxResults,
        CancellationToken ct = default)
    {
        var term = searchTerm.Trim();
        if (term.Length == 0)
            return [];

        var docs = await knowledgeDb.KnowledgeDocuments
            .AsNoTracking()
            .Where(d =>
                d.Title.Contains(term) ||
                (d.Summary != null && d.Summary.Contains(term)) ||
                d.Content.Contains(term))
            .OrderByDescending(d => d.Title.Contains(term))
            .Take(maxResults)
            .ToListAsync(ct);

        return docs.Select(d => new KnowledgeDocumentGroundingContext(
            DocumentId: d.Id.Value.ToString(),
            Title: d.Title,
            Summary: d.Summary,
            Category: d.Category.ToString())).ToList();
    }
}
