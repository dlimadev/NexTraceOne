using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Documento obtido de uma fonte de dados externa.
/// Unidade de conteúdo a ser embeddada e indexada no pipeline RAG.
/// </summary>
public sealed record DataSourceDocument(
    /// <summary>Título ou nome do documento/ficheiro/página.</summary>
    string Title,

    /// <summary>Conteúdo textual do documento (máx. recomendado: 8000 chars para caber num chunk).</summary>
    string Content,

    /// <summary>URL ou caminho de origem para referência e citação.</summary>
    string SourceUrl,

    /// <summary>Categoria opcional para filtragem (ex: "documentation", "runbook", "code").</summary>
    string? Category = null,

    /// <summary>Data de publicação ou modificação, quando disponível.</summary>
    DateTimeOffset? PublishedAt = null
);

/// <summary>
/// Conector de fonte de dados externa — abstracção que desacopla o tipo de fonte
/// da lógica de indexação e de busca em tempo de execução.
///
/// Cada implementação suporta um <see cref="ConnectorType"/> específico e pode implementar
/// uma ou ambas as estratégias de acesso ao conteúdo:
/// - <see cref="FetchDocumentsAsync"/> para indexação em batch (GitHub, GitLab, directório).
/// - <see cref="SearchAsync"/> para busca em tempo de query (Web Search APIs).
/// </summary>
public interface IDataSourceConnector
{
    /// <summary>Tipo de conector suportado por esta implementação.</summary>
    ExternalDataSourceConnectorType ConnectorType { get; }

    /// <summary>
    /// Indica se este conector suporta indexação em batch (pré-indexação de todo o conteúdo).
    /// True para GitHub, GitLab, directórios, Confluence, etc.
    /// </summary>
    bool SupportsIndexing { get; }

    /// <summary>
    /// Indica se este conector suporta busca em tempo real durante a query do utilizador.
    /// True para Web Search APIs (Brave, Serper, etc.).
    /// </summary>
    bool SupportsRuntimeSearch { get; }

    /// <summary>
    /// Obtém todos os documentos da fonte para indexação em batch.
    /// Apenas invocado quando <see cref="SupportsIndexing"/> é true.
    /// </summary>
    Task<IReadOnlyList<DataSourceDocument>> FetchDocumentsAsync(
        string connectorConfigJson,
        CancellationToken ct);

    /// <summary>
    /// Executa busca em tempo real com base na query do utilizador.
    /// Apenas invocado quando <see cref="SupportsRuntimeSearch"/> é true.
    /// </summary>
    Task<IReadOnlyList<DataSourceDocument>> SearchAsync(
        string connectorConfigJson,
        string query,
        int maxResults,
        CancellationToken ct);
}
