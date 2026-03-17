using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Repositório de sessões autenticadas do módulo Identity.
/// </summary>
public interface ISessionRepository
{
    /// <summary>Obtém uma sessão pelo identificador.</summary>
    Task<Session?> GetByIdAsync(SessionId id, CancellationToken cancellationToken);

    /// <summary>Obtém uma sessão pelo hash do refresh token.</summary>
    Task<Session?> GetByRefreshTokenHashAsync(RefreshTokenHash refreshTokenHash, CancellationToken cancellationToken);

    /// <summary>Obtém a sessão ativa mais recente de um usuário.</summary>
    Task<Session?> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken);

    /// <summary>Lista todas as sessões ativas de um usuário.</summary>
    Task<IReadOnlyList<Session>> ListActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova sessão para persistência.</summary>
    void Add(Session session);
}
