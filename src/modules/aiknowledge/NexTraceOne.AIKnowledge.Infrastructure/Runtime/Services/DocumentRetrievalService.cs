using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação real do serviço de retrieval de documentos para grounding de IA.
/// Pesquisa fontes de conhecimento registadas no módulo AIKnowledge (KnowledgeSources).
/// Filtra por query, classificação e fonte. Retorna resultados relevantes ou vazio honesto.
/// </summary>
public sealed class DocumentRetrievalService : IDocumentRetrievalService
{
    private readonly IAiKnowledgeSourceRepository _sourceRepo;
    private readonly ILogger<DocumentRetrievalService> _logger;

    public DocumentRetrievalService(
        IAiKnowledgeSourceRepository sourceRepo,
        ILogger<DocumentRetrievalService> logger)
    {
        _sourceRepo = sourceRepo;
        _logger = logger;
    }

    public async Task<DocumentSearchResult> SearchAsync(
        DocumentSearchRequest request,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Document retrieval requested for query '{Query}' with max {MaxResults} results",
            request.Query, request.MaxResults);

        try
        {
            var sources = await _sourceRepo.ListAsync(
                sourceType: null,
                isActive: true,
                ct: ct);

            if (sources.Count == 0)
            {
                _logger.LogDebug("No active knowledge sources found — returning empty result");
                return new DocumentSearchResult(Success: true, Hits: Array.Empty<DocumentSearchHit>());
            }

            var query = request.Query;
            var hits = sources
                .Where(s =>
                    s.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    s.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    s.EndpointOrPath.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Where(s => string.IsNullOrWhiteSpace(request.SourceFilter) ||
                    s.SourceType.ToString().Equals(request.SourceFilter, StringComparison.OrdinalIgnoreCase))
                .Take(request.MaxResults)
                .Select((s, index) => new DocumentSearchHit(
                    SourceId: s.SourceType.ToString(),
                    DocumentId: s.Id.Value.ToString(),
                    Title: s.Name,
                    Snippet: s.Description,
                    RelevanceScore: Math.Max(0.0, 1.0 - (index * 0.1)),
                    Classification: s.SourceType.ToString()))
                .ToList();

            _logger.LogDebug(
                "Document retrieval found {HitCount} results for query '{Query}'",
                hits.Count, request.Query);

            return new DocumentSearchResult(Success: true, Hits: hits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document retrieval failed for query '{Query}'", request.Query);
            return new DocumentSearchResult(Success: false, Hits: Array.Empty<DocumentSearchHit>(), ErrorMessage: ex.Message);
        }
    }
}
