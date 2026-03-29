using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Repositório para políticas de acesso granular por módulo/página/ação.
/// Permite consulta de permissões ao nível de ação individual no sistema,
/// com suporte a personalização por tenant para ambiente enterprise.
/// </summary>
public interface IModuleAccessPolicyRepository
{
    /// <summary>
    /// Verifica se um papel tem acesso permitido a um módulo/página/ação específico.
    /// Considera políticas do tenant (prioridade) e do sistema (fallback).
    /// </summary>
    /// <param name="roleId">Identificador do papel.</param>
    /// <param name="tenantId">Identificador do tenant (pode ser nulo para sistema).</param>
    /// <param name="module">Módulo da plataforma.</param>
    /// <param name="page">Página ou sub-área do módulo.</param>
    /// <param name="action">Ação específica.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>True se o acesso é permitido, false se negado, null se nenhuma política se aplica.</returns>
    Task<bool?> IsAllowedAsync(
        RoleId roleId,
        TenantId? tenantId,
        string module,
        string page,
        string action,
        CancellationToken cancellationToken);

    /// <summary>
    /// Obtém todas as políticas ativas para um papel num módulo, considerando tenant.
    /// </summary>
    Task<IReadOnlyList<ModuleAccessPolicy>> GetPoliciesForRoleAsync(
        RoleId roleId,
        TenantId? tenantId,
        string module,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verifica se existem políticas de acesso persistidas para um papel específico.
    /// Utilizado para determinar se deve usar seed do <see cref="ModuleAccessPolicyCatalog"/>.
    /// </summary>
    Task<bool> HasPoliciesForRoleAsync(RoleId roleId, TenantId? tenantId, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova política de acesso.</summary>
    Task AddAsync(ModuleAccessPolicy policy, CancellationToken cancellationToken);

    /// <summary>Adiciona várias políticas de acesso em lote.</summary>
    Task AddRangeAsync(IEnumerable<ModuleAccessPolicy> policies, CancellationToken cancellationToken);
}
