using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

/// <summary>
/// Implementação nula do repositório de search.
/// Usada quando o usuário NÃO escolheu ElasticSearch durante a instalação.
/// Retorna coleções vazias e resultados padrão para evitar NullReferenceException.
/// </summary>
internal sealed class NullAiSearchRepository : IAiSearchRepository
{
    private static readonly Task<SearchResult<PromptTemplateDocument>> EmptyPromptResults = 
        Task.FromResult(new SearchResult<PromptTemplateDocument>(new List<PromptTemplateDocument>(), 0, 1, 20, 0.0));
    
    private static readonly Task<SearchResult<ConversationDocument>> EmptyConversationResults = 
        Task.FromResult(new SearchResult<ConversationDocument>(new List<ConversationDocument>(), 0, 1, 20, 0.0));
    
    private static readonly Task<SearchResult<KnowledgeDocumentDocument>> EmptyKnowledgeResults = 
        Task.FromResult(new SearchResult<KnowledgeDocumentDocument>(new List<KnowledgeDocumentDocument>(), 0, 1, 20, 0.0));

    public Task IndexPromptTemplateAsync(PromptTemplateDocument document)
    {
        // No-op: ElasticSearch não está configurado
        return Task.CompletedTask;
    }

    public Task IndexConversationAsync(ConversationDocument document)
    {
        // No-op: ElasticSearch não está configurado
        return Task.CompletedTask;
    }

    public Task IndexKnowledgeDocumentAsync(KnowledgeDocumentDocument document)
    {
        // No-op: ElasticSearch não está configurado
        return Task.CompletedTask;
    }

    public Task<SearchResult<PromptTemplateDocument>> SearchPromptsAsync(
        string query,
        int page = 1,
        int pageSize = 20,
        Guid? tenantId = null,
        string[]? categories = null)
    {
        return EmptyPromptResults;
    }

    public Task<SearchResult<ConversationDocument>> SearchConversationsAsync(
        string query,
        int page = 1,
        int pageSize = 20,
        Guid? tenantId = null,
        DateTime? from = null,
        DateTime? to = null)
    {
        return EmptyConversationResults;
    }

    public Task<SearchResult<KnowledgeDocumentDocument>> SearchKnowledgeAsync(
        string query,
        int page = 1,
        int pageSize = 20,
        Guid? tenantId = null,
        string[]? tags = null)
    {
        return EmptyKnowledgeResults;
    }

    public Task DeleteDocumentAsync(string indexName, string documentId)
    {
        // No-op: ElasticSearch não está configurado
        return Task.CompletedTask;
    }
}
