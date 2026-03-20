using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de sessões autenticadas persistidas via EF Core.
/// Busca por refresh token usa o Value do VO para tradução correta em SQL.
/// </summary>
internal sealed class SessionRepository(IdentityDbContext context)
    : RepositoryBase<Session, SessionId>(context), ISessionRepository
{
    /// <inheritdoc />
    public async Task<Session?> GetByRefreshTokenHashAsync(RefreshTokenHash refreshTokenHash, CancellationToken cancellationToken)
        => await context.Sessions.SingleOrDefaultAsync(x => x.RefreshToken == refreshTokenHash, cancellationToken);

    /// <inheritdoc />
    public async Task<Session?> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken)
        => await context.Sessions
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .OrderByDescending(x => x.ExpiresAt)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Session>> ListActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken)
        => await context.Sessions
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .OrderByDescending(x => x.ExpiresAt)
            .ToListAsync(cancellationToken);
}
