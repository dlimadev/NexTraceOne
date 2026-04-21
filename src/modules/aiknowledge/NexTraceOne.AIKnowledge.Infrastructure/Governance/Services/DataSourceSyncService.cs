using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Services;

/// <summary>
/// Orquestra o ciclo completo de sincronização de fontes de dados externas:
/// fetch → embed → persist como AIKnowledgeSource no pipeline RAG.
///
/// Fontes com SupportsRuntimeSearch usam SearchAsync em tempo de query
/// (sem pré-indexação). Fontes com SupportsIndexing são indexadas em batch.
/// </summary>
internal sealed class DataSourceSyncService(
    IDataSourceConnectorFactory connectorFactory,
    IAiKnowledgeSourceRepository knowledgeSourceRepository,
    IEmbeddingCacheService embeddingCache,
    ILogger<DataSourceSyncService> logger) : IDataSourceSyncService
{
    public async Task<DataSourceSyncResult> SyncAsync(ExternalDataSource source, CancellationToken ct)
    {
        try
        {
            var connector = connectorFactory.GetConnector(source.ConnectorType);

            if (!connector.SupportsIndexing)
            {
                // Runtime-search sources (e.g., BraveSearch) don't need pre-indexing.
                source.RecordSyncSuccess(0, DateTimeOffset.UtcNow);
                return new DataSourceSyncResult(Success: true, DocumentsIndexed: 0);
            }

            logger.LogInformation(
                "DataSourceSyncService: starting sync for source '{Name}' ({Type}).",
                source.Name, source.ConnectorType);

            var documents = await connector.FetchDocumentsAsync(source.ConnectorConfigJson, ct);

            if (documents.Count == 0)
            {
                source.RecordSyncSuccess(0, DateTimeOffset.UtcNow);
                logger.LogInformation("DataSourceSyncService: no documents fetched for '{Name}'.", source.Name);
                return new DataSourceSyncResult(Success: true, DocumentsIndexed: 0);
            }

            var indexed = 0;
            foreach (var doc in documents)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await IndexDocumentAsync(source, doc, ct);
                    indexed++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "DataSourceSyncService: failed to index document '{Title}'.", doc.Title);
                }
            }

            source.RecordSyncSuccess(indexed, DateTimeOffset.UtcNow);

            logger.LogInformation(
                "DataSourceSyncService: sync complete for '{Name}' — {Count}/{Total} documents indexed.",
                source.Name, indexed, documents.Count);

            return new DataSourceSyncResult(Success: true, DocumentsIndexed: indexed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DataSourceSyncService: sync failed for source '{Name}'.", source.Name);
            source.RecordSyncError(ex.Message, DateTimeOffset.UtcNow);
            return new DataSourceSyncResult(Success: false, DocumentsIndexed: 0, ErrorMessage: ex.Message);
        }
    }

    public async Task<IReadOnlyList<DataSourceDocument>> SearchAsync(
        ExternalDataSource source,
        string query,
        int maxResults,
        CancellationToken ct)
    {
        try
        {
            var connector = connectorFactory.GetConnector(source.ConnectorType);
            if (!connector.SupportsRuntimeSearch)
                return [];

            return await connector.SearchAsync(source.ConnectorConfigJson, query, maxResults, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "DataSourceSyncService: runtime search failed for source '{Name}'.", source.Name);
            return [];
        }
    }

    private async Task IndexDocumentAsync(ExternalDataSource source, DataSourceDocument doc, CancellationToken ct)
    {
        // Derive a unique name combining source + doc title (truncated to 200 chars)
        var sourceName = $"[{source.ConnectorType}] {doc.Title}";
        if (sourceName.Length > 200) sourceName = sourceName[..200];

        // Map connector type to closest KnowledgeSourceType
        var knowledgeSourceType = source.ConnectorType switch
        {
            ExternalDataSourceConnectorType.GitHub => KnowledgeSourceType.Documentation,
            ExternalDataSourceConnectorType.GitLab => KnowledgeSourceType.Documentation,
            ExternalDataSourceConnectorType.LocalDirectory => KnowledgeSourceType.Runbook,
            ExternalDataSourceConnectorType.WebSearch => KnowledgeSourceType.SourceOfTruth,
            ExternalDataSourceConnectorType.Confluence => KnowledgeSourceType.Documentation,
            ExternalDataSourceConnectorType.Notion => KnowledgeSourceType.Documentation,
            ExternalDataSourceConnectorType.AzureDevOps => KnowledgeSourceType.Documentation,
            _ => KnowledgeSourceType.Documentation
        };

        var description = doc.Content.Length > 500 ? doc.Content[..500] : doc.Content;

        var knowledgeSource = AIKnowledgeSource.Register(
            name: sourceName,
            description: description,
            sourceType: knowledgeSourceType,
            endpointOrPath: doc.SourceUrl,
            priority: 50,
            registeredAt: DateTimeOffset.UtcNow);

        // Generate embedding for the document content
        var textToEmbed = $"{doc.Title}\n\n{doc.Content}";
        if (textToEmbed.Length > 8000) textToEmbed = textToEmbed[..8000];

        var embedding = await embeddingCache.GetOrComputeAsync(textToEmbed, ct);
        if (embedding.Length > 0)
            knowledgeSource.SetEmbedding(embedding);

        // Persist to the shared knowledge source table (reuses existing RAG pipeline)
        await knowledgeSourceRepository.StoreKnowledgeSourceAsync(knowledgeSource, ct);
    }
}
