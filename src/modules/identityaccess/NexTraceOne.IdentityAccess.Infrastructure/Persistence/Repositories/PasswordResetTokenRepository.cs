using Microsoft.EntityFrameworkCore;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Repositories;

internal sealed class PasswordResetTokenRepository(IdentityDbContext context)
    : IPasswordResetTokenRepository
{
    public Task<PasswordResetToken?> FindByHashAsync(string tokenHash, CancellationToken ct)
        => context.PasswordResetTokens
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, ct);

    public async Task DeleteByUserIdAsync(UserId userId, CancellationToken ct)
    {
        var existing = await context.PasswordResetTokens
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);
        context.PasswordResetTokens.RemoveRange(existing);
    }

    public void Add(PasswordResetToken token)
        => context.PasswordResetTokens.Add(token);
}
