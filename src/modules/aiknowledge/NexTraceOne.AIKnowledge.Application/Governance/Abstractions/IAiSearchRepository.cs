namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Interface para repositório de search de IA usando ElasticSearch.
/// Fornece busca full-text avançada em prompts, conversas e conhecimento.
/// 
/// Esta interface é implementada apenas quando o usuário escolhe ElasticSearch durante a instalação.
/// Se não configurado, usa-se NullAiSearchRepository (retorna coleções vazias).
/// </summary>
public interface IAiSearchRepository
{
    /// <summary>Indexa um prompt template no ElasticSearch.</summary>
    Task IndexPromptTemplateAsync(PromptTemplateDocument document);

    /// <summary>Indexa uma conversa no ElasticSearch.</summary>
    Task IndexConversationAsync(ConversationDocument document);

    /// <summary>Indexa um documento de conhecimento no ElasticSearch.</summary>
    Task IndexKnowledgeDocumentAsync(KnowledgeDocumentDocument document);

    /// <summary>Busca prompts por texto com relevância e filtros.</summary>
    Task<SearchResult<PromptTemplateDocument>> SearchPromptsAsync(
        string query,
        int page = 1,
        int pageSize = 20,
        Guid? tenantId = null,
        string[]? categories = null);

    /// <summary>Busca conversas por texto com relevância e filtros.</summary>
    Task<SearchResult<ConversationDocument>> SearchConversationsAsync(
        string query,
        int page = 1,
        int pageSize = 20,
        Guid? tenantId = null,
        DateTime? from = null,
        DateTime? to = null);

    /// <summary>Busca documentos de conhecimento por texto com relevância.</summary>
    Task<SearchResult<KnowledgeDocumentDocument>> SearchKnowledgeAsync(
        string query,
        int page = 1,
        int pageSize = 20,
        Guid? tenantId = null,
        string[]? tags = null);

    /// <summary>Remove um documento do índice.</summary>
    Task DeleteDocumentAsync(string indexName, string documentId);
}

/// <summary>
/// Documento de prompt template para indexação no ElasticSearch.
/// </summary>
public sealed record PromptTemplateDocument(
    string Id,
    Guid TenantId,
    string Name,
    string Description,
    string Content,
    string Category,
    string[] Tags,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// Documento de conversa para indexação no ElasticSearch.
/// </summary>
public sealed record ConversationDocument(
    string Id,
    Guid TenantId,
    Guid AgentId,
    string AgentName,
    string UserQuery,
    string AiResponse,
    string[] Messages,
    DateTime CreatedAt,
    int MessageCount);

/// <summary>
/// Documento de conhecimento para indexação no ElasticSearch.
/// </summary>
public sealed record KnowledgeDocumentDocument(
    string Id,
    Guid TenantId,
    string Title,
    string Content,
    string SourceType,
    string SourceUrl,
    string[] Tags,
    DateTime IndexedAt);

/// <summary>
/// Resultado de busca com paginação e metadados.
/// </summary>
public sealed record SearchResult<T>(
    List<T> Items,
    long TotalCount,
    int Page,
    int PageSize,
    double MaxScore);
