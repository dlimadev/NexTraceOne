using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Resultado de uma sincronização de fonte de dados externa.
/// </summary>
public sealed record DataSourceSyncResult(
    bool Success,
    int DocumentsIndexed,
    string? ErrorMessage = null
);

/// <summary>
/// Orquestra o ciclo completo de sincronização de uma fonte de dados externa:
/// fetch → chunk → embed → persist como AIKnowledgeSource no pipeline RAG.
/// </summary>
public interface IDataSourceSyncService
{
    /// <summary>
    /// Executa a sincronização completa de uma fonte.
    /// Internamente: obtém documentos via <see cref="IDataSourceConnector.FetchDocumentsAsync"/>,
    /// gera embeddings via <see cref="IEmbeddingProvider"/> e persiste como <see cref="AIKnowledgeSource"/>.
    /// </summary>
    Task<DataSourceSyncResult> SyncAsync(
        ExternalDataSource source,
        CancellationToken ct);

    /// <summary>
    /// Executa busca em tempo real numa fonte com suporte a RuntimeSearch.
    /// Usado pelo pipeline de grounding durante a query do utilizador.
    /// </summary>
    Task<IReadOnlyList<DataSourceDocument>> SearchAsync(
        ExternalDataSource source,
        string query,
        int maxResults,
        CancellationToken ct);
}
