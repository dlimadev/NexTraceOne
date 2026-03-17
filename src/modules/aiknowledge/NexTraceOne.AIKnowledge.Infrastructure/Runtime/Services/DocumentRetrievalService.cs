using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação stub do serviço de retrieval de documentos.
/// Fundação para futuro RAG com embeddings e pesquisa semântica.
/// Retorna resultados vazios até integração com fontes documentais reais
/// (Confluence, SharePoint, vector store, etc.).
/// </summary>
public sealed class DocumentRetrievalService : IDocumentRetrievalService
{
    private readonly ILogger<DocumentRetrievalService> _logger;

    public DocumentRetrievalService(ILogger<DocumentRetrievalService> logger)
    {
        _logger = logger;
    }

    public Task<DocumentSearchResult> SearchAsync(
        DocumentSearchRequest request,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Document retrieval requested for query '{Query}' with max {MaxResults} results — full implementation pending (RAG/embedding integration required)",
            request.Query, request.MaxResults);

        var result = new DocumentSearchResult(
            Success: true,
            Hits: Array.Empty<DocumentSearchHit>(),
            ErrorMessage: null);

        return Task.FromResult(result);
    }
}
