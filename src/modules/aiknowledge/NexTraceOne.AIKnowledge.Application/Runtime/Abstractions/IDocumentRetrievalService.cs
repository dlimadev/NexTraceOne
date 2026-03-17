namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Serviço de retrieval de documentos para grounding de IA.
/// Abstrai a pesquisa em fontes documentais (Confluence, SharePoint, arquivos indexados, etc.).
/// Fundação para futuro RAG com embeddings e pesquisa semântica.
/// </summary>
public interface IDocumentRetrievalService
{
    /// <summary>Pesquisa documentos relevantes para grounding de IA.</summary>
    Task<DocumentSearchResult> SearchAsync(
        DocumentSearchRequest request,
        CancellationToken ct = default);
}

/// <summary>Request de pesquisa de documentos.</summary>
public sealed record DocumentSearchRequest(
    string Query,
    int MaxResults = 10,
    string? SourceFilter = null,
    string? ClassificationFilter = null);

/// <summary>Resultado de pesquisa de documentos.</summary>
public sealed record DocumentSearchResult(
    bool Success,
    IReadOnlyList<DocumentSearchHit> Hits,
    string? ErrorMessage = null);

/// <summary>Hit individual de pesquisa de documentos.</summary>
public sealed record DocumentSearchHit(
    string SourceId,
    string DocumentId,
    string Title,
    string Snippet,
    double RelevanceScore,
    string? Classification = null);
