using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Contracts;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Infrastructure.Search;

/// <summary>
/// Implementação do IKnowledgeSearchProvider que pesquisa documentos e notas
/// operacionais usando PostgreSQL Full Text Search (FTS) via os repositórios
/// do módulo Knowledge.
///
/// P10.2: Motor inicial de search cross-module.
/// Evolução futura: colunas tsvector materializadas + GIN indexes para otimização.
/// </summary>
internal sealed class KnowledgeSearchProvider(
    IKnowledgeDocumentRepository documentRepository,
    IOperationalNoteRepository noteRepository,
    IKnowledgeRelationRepository relationRepository) : IKnowledgeSearchProvider
{
    public async Task<IReadOnlyList<KnowledgeSearchResultItem>> SearchAsync(
        string searchTerm,
        string? scope,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var results = new List<KnowledgeSearchResultItem>();
        var scopeAll = string.IsNullOrWhiteSpace(scope) || scope == "all";

        // Pesquisar documentos de conhecimento
        if (scopeAll || scope == "knowledge")
        {
            var documents = await documentRepository.SearchAsync(searchTerm, maxResults, cancellationToken);
            foreach (var doc in documents)
            {
                var score = CalculateRelevance(searchTerm, doc.Title, doc.Summary);
                var relationContext = await BuildRelationContextAsync(doc.Id.Value, cancellationToken);
                results.Add(new KnowledgeSearchResultItem(
                    doc.Id.Value,
                    "knowledge",
                    doc.Title,
                    relationContext is null
                        ? doc.Summary ?? $"{doc.Category} · v{doc.Version}"
                        : $"{doc.Summary ?? $"{doc.Category} · v{doc.Version}"} · {relationContext}",
                    doc.Status.ToString(),
                    $"/knowledge/documents/{doc.Id.Value}",
                    score));
            }
        }

        // Pesquisar notas operacionais
        if (scopeAll || scope == "notes")
        {
            var notes = await noteRepository.SearchAsync(searchTerm, maxResults, cancellationToken);
            foreach (var note in notes)
            {
                var score = CalculateRelevance(searchTerm, note.Title, note.Content);
                var contextInfo = note.ContextType is not null
                    ? $"{note.ContextType} · {note.Severity}"
                    : note.Severity.ToString();
                var relationContext = await BuildRelationContextAsync(note.Id.Value, cancellationToken);
                results.Add(new KnowledgeSearchResultItem(
                    note.Id.Value,
                    "note",
                    note.Title,
                    relationContext is null ? contextInfo : $"{contextInfo} · {relationContext}",
                    note.IsResolved ? "Resolved" : note.Severity.ToString(),
                    $"/knowledge/notes/{note.Id.Value}",
                    score));
            }
        }

        return results
            .OrderByDescending(r => r.RelevanceScore)
            .Take(maxResults)
            .ToList();
    }

    /// <summary>
    /// Calcula relevância simples: correspondência exata no campo primário gera
    /// pontuação mais alta; correspondência parcial gera pontuação intermédia.
    /// Alinhado com o algoritmo usado no GlobalSearch do Catalog.
    /// </summary>
    private static double CalculateRelevance(
        string searchTerm,
        string primaryField,
        string? secondaryField)
    {
        var score = 0.0;

        if (!string.IsNullOrWhiteSpace(primaryField))
        {
            if (primaryField.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
                score += 1.0;
            else if (primaryField.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                score += 0.7;
        }

        if (!string.IsNullOrWhiteSpace(secondaryField))
        {
            if (secondaryField.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
                score += 0.5;
            else if (secondaryField.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                score += 0.3;
        }

        return score;
    }

    private async Task<string?> BuildRelationContextAsync(Guid sourceEntityId, CancellationToken cancellationToken)
    {
        var relations = await relationRepository.ListBySourceAsync(sourceEntityId, cancellationToken);
        if (relations.Count == 0)
            return null;

        var labels = relations
            .Select(r => r.TargetType.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray();

        return labels.Length == 0 ? null : $"linked:{string.Join("/", labels)}";
    }
}
