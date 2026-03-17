using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de políticas de quota de tokens para controlo de consumo de IA.
/// Suporta consulta individual, listagem, filtragem por escopo/utilizador/tenant e persistência.
/// </summary>
public interface IAiTokenQuotaPolicyRepository
{
    /// <summary>Obtém uma política pelo identificador fortemente tipado.</summary>
    Task<AiTokenQuotaPolicy?> GetByIdAsync(AiTokenQuotaPolicyId id, CancellationToken ct = default);

    /// <summary>Lista todas as políticas de quota registadas.</summary>
    Task<IReadOnlyList<AiTokenQuotaPolicy>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Lista políticas filtradas por escopo (ex: "user", "tenant", "provider").</summary>
    Task<IReadOnlyList<AiTokenQuotaPolicy>> GetByScopeAsync(string scope, CancellationToken ct = default);

    /// <summary>Lista políticas aplicáveis a um utilizador específico.</summary>
    Task<IReadOnlyList<AiTokenQuotaPolicy>> GetForUserAsync(string userId, CancellationToken ct = default);

    /// <summary>Lista políticas aplicáveis a um tenant específico.</summary>
    Task<IReadOnlyList<AiTokenQuotaPolicy>> GetForTenantAsync(string tenantId, CancellationToken ct = default);

    /// <summary>Adiciona uma nova política para persistência.</summary>
    Task AddAsync(AiTokenQuotaPolicy entity, CancellationToken ct = default);
}
