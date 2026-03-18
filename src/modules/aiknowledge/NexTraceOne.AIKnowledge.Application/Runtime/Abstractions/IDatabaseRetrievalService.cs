namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Serviço de retrieval de dados estruturados para grounding de IA.
/// Abstrai a pesquisa em fontes de dados internas (modelos de IA, contratos, serviços, etc.).
/// Acesso controlado e governado — nunca executa SQL arbitrário.
/// </summary>
public interface IDatabaseRetrievalService
{
    /// <summary>Pesquisa dados estruturados relevantes para grounding de IA.</summary>
    Task<DatabaseSearchResult> SearchAsync(
        DatabaseSearchRequest request,
        CancellationToken ct = default);
}

/// <summary>Request de pesquisa de dados estruturados.</summary>
public sealed record DatabaseSearchRequest(
    string Query,
    string? EntityType = null,
    string? TenantId = null,
    int MaxResults = 10);

/// <summary>Resultado de pesquisa de dados estruturados.</summary>
public sealed record DatabaseSearchResult(
    bool Success,
    IReadOnlyList<DatabaseSearchHit> Hits,
    string? ErrorMessage = null);

/// <summary>Hit individual de pesquisa de dados estruturados.</summary>
public sealed record DatabaseSearchHit(
    string EntityType,
    string EntityId,
    string DisplayName,
    string Summary,
    double RelevanceScore);
