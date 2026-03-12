using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.ValueObjects;

namespace NexTraceOne.Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de sessões autenticadas persistidas via EF Core.
/// Busca por refresh token usa o Value do VO para tradução correta em SQL.
/// </summary>
internal sealed class SessionRepository(IdentityDbContext context)
    : RepositoryBase<Session, SessionId>(context), ISessionRepository
{
    /// <inheritdoc />
    public async Task<Session?> GetByRefreshTokenHashAsync(RefreshTokenHash refreshTokenHash, CancellationToken cancellationToken)
        => await context.Sessions.SingleOrDefaultAsync(x => x.RefreshToken.Value == refreshTokenHash.Value, cancellationToken);

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
