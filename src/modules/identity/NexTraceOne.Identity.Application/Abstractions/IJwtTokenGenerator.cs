using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Application.Abstractions;

/// <summary>
/// Serviço responsável por gerar access tokens e refresh tokens do módulo Identity.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>Tempo de expiração do access token em segundos.</summary>
    int AccessTokenLifetimeSeconds { get; }

    /// <summary>Gera um access token JWT para o usuário autenticado.</summary>
    string GenerateAccessToken(User user, TenantMembership membership, IReadOnlyCollection<string> permissions);

    /// <summary>Gera um refresh token aleatório em texto plano.</summary>
    string GenerateRefreshToken();
}
