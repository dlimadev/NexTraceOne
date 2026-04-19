using NexTraceOne.ProductAnalytics.Domain.Entities;

namespace NexTraceOne.ProductAnalytics.Application.Abstractions;

/// <summary>
/// Repositório para gestão de definições de jornadas configuráveis.
/// Suporta scope global (tenant null) e personalização por tenant.
/// </summary>
public interface IJourneyDefinitionRepository
{
    /// <summary>
    /// Lista todas as definições activas para o tenant dado,
    /// fundindo as globais com as específicas do tenant (as do tenant têm prioridade).
    /// </summary>
    Task<IReadOnlyList<JourneyDefinition>> ListActiveAsync(Guid? tenantId, CancellationToken ct);

    /// <summary>Retorna uma definição pelo seu ID.</summary>
    Task<JourneyDefinition?> GetByIdAsync(JourneyDefinitionId id, CancellationToken ct);

    /// <summary>Retorna uma definição pela sua key + tenant scope.</summary>
    Task<JourneyDefinition?> GetByKeyAsync(string key, Guid? tenantId, CancellationToken ct);

    /// <summary>Verifica se já existe uma definição com a mesma key no scope dado.</summary>
    Task<bool> ExistsAsync(string key, Guid? tenantId, CancellationToken ct);

    /// <summary>Persiste uma nova definição.</summary>
    Task AddAsync(JourneyDefinition definition, CancellationToken ct);

    /// <summary>Remove logicamente uma definição (delete físico permitido para gestão).</summary>
    void Remove(JourneyDefinition definition);
}
