using Elasticsearch.Net;
using Nest;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.ElasticSearch;

/// <summary>
/// Implementação do repositório de search usando ElasticSearch.
/// Fornece busca full-text avançada com relevância, filtros e paginação.
/// </summary>
internal sealed class ElasticSearchAiRepository : IAiSearchRepository
{
    private readonly IElasticClient _client;

    public ElasticSearchAiRepository(string connectionString)
    {
        var uri = new Uri(connectionString);
        var settings = new ConnectionSettings(uri)
            .DefaultIndex("ai-search")
            .PrettyJson();
        
        _client = new ElasticClient(settings);
    }

    public async Task IndexPromptTemplateAsync(PromptTemplateDocument document)
    {
        var response = await _client.IndexAsync(document, idx => idx
            .Index("prompt-templates")
            .Id(document.Id));

        if (!response.IsValid)
        {
            throw new InvalidOperationException($"Failed to index prompt template: {response.ServerError?.Error.Reason}");
        }
    }

    public async Task IndexConversationAsync(ConversationDocument document)
    {
        var response = await _client.IndexAsync(document, idx => idx
            .Index("conversations")
            .Id(document.Id));

        if (!response.IsValid)
        {
            throw new InvalidOperationException($"Failed to index conversation: {response.ServerError?.Error.Reason}");
        }
    }

    public async Task IndexKnowledgeDocumentAsync(KnowledgeDocumentDocument document)
    {
        var response = await _client.IndexAsync(document, idx => idx
            .Index("knowledge-documents")
            .Id(document.Id));

        if (!response.IsValid)
        {
            throw new InvalidOperationException($"Failed to index knowledge document: {response.ServerError?.Error.Reason}");
        }
    }

    public async Task<SearchResult<PromptTemplateDocument>> SearchPromptsAsync(
        string query,
        int page = 1,
        int pageSize = 20,
        Guid? tenantId = null,
        string[]? categories = null)
    {
        var searchDescriptor = new SearchDescriptor<PromptTemplateDocument>()
            .Index("prompt-templates")
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Query(q => q
                .Bool(b =>
                {
                    b.Must(m => m
                        .MultiMatch(mm => mm
                            .Query(query)
                            .Fields(f => f
                                .Field(p => p.Name, 2.0)
                                .Field(p => p.Description, 1.5)
                                .Field(p => p.Content, 1.0)
                                .Field(p => p.Tags, 1.2))));

                    if (tenantId.HasValue)
                    {
                        b.Filter(f => f.Term(t => t.TenantId, tenantId.Value));
                    }

                    if (categories != null && categories.Length > 0)
                    {
                        b.Filter(f => f.Terms(t => t.Field(p => p.Category).Terms(categories.Cast<object>())));
                    }

                    return b;
                }));

        var response = await _client.SearchAsync<PromptTemplateDocument>(searchDescriptor);

        return new SearchResult<PromptTemplateDocument>(
            Items: response.Documents.ToList(),
            TotalCount: response.Total,
            Page: page,
            PageSize: pageSize,
            MaxScore: response.MaxScore);
    }

    public async Task<SearchResult<ConversationDocument>> SearchConversationsAsync(
        string query,
        int page = 1,
        int pageSize = 20,
        Guid? tenantId = null,
        DateTime? from = null,
        DateTime? to = null)
    {
        var searchDescriptor = new SearchDescriptor<ConversationDocument>()
            .Index("conversations")
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Query(q => q
                .Bool(b =>
                {
                    b.Must(m => m
                        .MultiMatch(mm => mm
                            .Query(query)
                            .Fields(f => f
                                .Field(c => c.UserQuery, 2.0)
                                .Field(c => c.AiResponse, 1.5)
                                .Field(c => c.Messages, 1.0))));

                    if (tenantId.HasValue)
                    {
                        b.Filter(f => f.Term(t => t.TenantId, tenantId.Value));
                    }

                    if (from.HasValue || to.HasValue)
                    {
                        b.Filter(f => f.DateRange(r => r
                            .Field(c => c.CreatedAt)
                            .GreaterThanOrEquals(from)
                            .LessThanOrEquals(to)));
                    }

                    return b;
                }));

        var response = await _client.SearchAsync<ConversationDocument>(searchDescriptor);

        return new SearchResult<ConversationDocument>(
            Items: response.Documents.ToList(),
            TotalCount: response.Total,
            Page: page,
            PageSize: pageSize,
            MaxScore: response.MaxScore);
    }

    public async Task<SearchResult<KnowledgeDocumentDocument>> SearchKnowledgeAsync(
        string query,
        int page = 1,
        int pageSize = 20,
        Guid? tenantId = null,
        string[]? tags = null)
    {
        var searchDescriptor = new SearchDescriptor<KnowledgeDocumentDocument>()
            .Index("knowledge-documents")
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Query(q => q
                .Bool(b =>
                {
                    b.Must(m => m
                        .MultiMatch(mm => mm
                            .Query(query)
                            .Fields(f => f
                                .Field(k => k.Title, 2.0)
                                .Field(k => k.Content, 1.0)
                                .Field(k => k.Tags, 1.5))));

                    if (tenantId.HasValue)
                    {
                        b.Filter(f => f.Term(t => t.TenantId, tenantId.Value));
                    }

                    if (tags != null && tags.Length > 0)
                    {
                        b.Filter(f => f.Terms(t => t.Field(k => k.Tags).Terms(tags.Cast<object>())));
                    }

                    return b;
                }));

        var response = await _client.SearchAsync<KnowledgeDocumentDocument>(searchDescriptor);

        return new SearchResult<KnowledgeDocumentDocument>(
            Items: response.Documents.ToList(),
            TotalCount: response.Total,
            Page: page,
            PageSize: pageSize,
            MaxScore: response.MaxScore);
    }

    public async Task DeleteDocumentAsync(string indexName, string documentId)
    {
        var response = await _client.DeleteAsync<object>(documentId, idx => idx.Index(indexName));

        if (!response.IsValid)
        {
            throw new InvalidOperationException($"Failed to delete document: {response.ServerError?.Error.Reason}");
        }
    }
}
