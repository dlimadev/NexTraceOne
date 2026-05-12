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
    /// Claims: sub, email, name, tenant_id, role_ids[] (multi-valued), role_names[] (multi-valued), capabilities[].
    /// As permissões são derivadas server-side via IClaimsTransformation a partir dos role_names,
    /// mantendo o token pequeno o suficiente para caber num cookie HttpOnly (≤ 4 KB).
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="tenantId">Tenant selecionado.</param>
    /// <param name="roleIds">Identificadores dos papéis ativos (GUIDs).</param>
    /// <param name="roleNames">Nomes dos papéis ativos (ex.: "platform_admin").</param>
    /// <param name="capabilities">Capabilities do plano SaaS do tenant (opcional).</param>
    string GenerateAccessToken(User user, TenantId tenantId, IReadOnlyCollection<RoleId> roleIds, IReadOnlyCollection<string> roleNames, IReadOnlyCollection<string>? capabilities = null);

    /// <summary>Gera um refresh token aleatório em texto plano.</summary>
    string GenerateRefreshToken();
}
