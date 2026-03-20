using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Abstração para resolução do contexto operacional completo (TenantEnvironmentContext)
/// a partir do tenant e ambiente ativos na requisição.
///
/// A resolução do contexto é uma operação crítica:
/// - Valida que o tenant existe e está ativo
/// - Valida que o ambiente pertence ao tenant
/// - Valida que o ambiente está ativo
/// - Retorna o contexto completo com perfil e criticidade
///
/// Implementações devem usar cache de curta duração (ex.: por requisição via IMemoryCache
/// ou scoped DI) para evitar consultas repetidas ao banco durante uma única operação.
/// </summary>
public interface ITenantEnvironmentContextResolver
{
    /// <summary>
    /// Resolve o contexto operacional completo para a combinação de tenant e ambiente informados.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="environmentId">Identificador do ambiente.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O TenantEnvironmentContext resolvido, ou null se o ambiente não existir ou não pertencer ao tenant.</returns>
    Task<TenantEnvironmentContext?> ResolveAsync(
        TenantId tenantId,
        EnvironmentId environmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista os contextos de todos os ambientes ativos de um tenant.
    /// Usado para operações cross-environment como comparação e análise de promoção.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<IReadOnlyList<TenantEnvironmentContext>> ListActiveContextsForTenantAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default);
}
