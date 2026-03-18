using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação do serviço de retrieval de dados estruturados para grounding de IA.
/// Demonstra padrão de acesso controlado e governado aos dados internos da plataforma.
/// Usa IAiModelRepository como proof of concept — pesquisa modelos por keyword.
/// Nunca executa SQL arbitrário — apenas queries pré-definidas nos repositórios.
/// </summary>
public sealed class DatabaseRetrievalService : IDatabaseRetrievalService
{
    private readonly IAiModelRepository _modelRepository;
    private readonly ILogger<DatabaseRetrievalService> _logger;

    public DatabaseRetrievalService(
        IAiModelRepository modelRepository,
        ILogger<DatabaseRetrievalService> logger)
    {
        _modelRepository = modelRepository;
        _logger = logger;
    }

    public async Task<DatabaseSearchResult> SearchAsync(
        DatabaseSearchRequest request,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Database retrieval requested for query '{Query}', entity type '{EntityType}', max {MaxResults} results",
            request.Query, request.EntityType, request.MaxResults);

        try
        {
            // Proof of concept: pesquisa modelos de IA por keyword usando repositório governado.
            // Futuramente será expandido para contratos, serviços, incidentes, etc.
            var models = await _modelRepository.ListAsync(
                provider: null,
                modelType: null,
                status: ModelStatus.Active,
                isInternal: null,
                ct: ct);

            var query = request.Query;
            var hits = models
                .Where(m =>
                    m.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    m.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    m.Provider.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(request.MaxResults)
                .Select((m, index) => new DatabaseSearchHit(
                    EntityType: "AIModel",
                    EntityId: m.Id.Value.ToString(),
                    DisplayName: m.DisplayName,
                    Summary: $"AI Model '{m.Name}' from provider '{m.Provider}' — {m.Capabilities}",
                    RelevanceScore: Math.Max(0.0, 1.0 - (index * 0.1))))
                .ToList();

            _logger.LogDebug(
                "Database retrieval found {HitCount} results for query '{Query}'",
                hits.Count, request.Query);

            return new DatabaseSearchResult(true, hits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database retrieval failed for query '{Query}'", request.Query);
            return new DatabaseSearchResult(false, Array.Empty<DatabaseSearchHit>(), ex.Message);
        }
    }
}
