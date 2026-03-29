using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Serviço responsável por gerar access tokens e refresh tokens do módulo Identity.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>Tempo de expiração do access token em segundos.</summary>
    int AccessTokenLifetimeSeconds { get; }

    /// <summary>
    /// Gera um access token JWT para o usuário autenticado com um único papel.
    /// Mantido para compatibilidade retroativa com fluxos existentes.
    /// </summary>
    string GenerateAccessToken(User user, TenantMembership membership, IReadOnlyCollection<string> permissions);

    /// <summary>
    /// Gera um access token JWT para o usuário autenticado com múltiplos papéis.
    /// Claims: sub, email, name, tenant_id, role_ids[] (multi-valued), permissions[].
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="tenantId">Tenant selecionado.</param>
    /// <param name="roleIds">Identificadores dos papéis ativos.</param>
    /// <param name="permissions">Permissões efetivas (união de todos os papéis).</param>
    string GenerateAccessToken(User user, TenantId tenantId, IReadOnlyCollection<RoleId> roleIds, IReadOnlyCollection<string> permissions);

    /// <summary>Gera um refresh token aleatório em texto plano.</summary>
    string GenerateRefreshToken();
}
