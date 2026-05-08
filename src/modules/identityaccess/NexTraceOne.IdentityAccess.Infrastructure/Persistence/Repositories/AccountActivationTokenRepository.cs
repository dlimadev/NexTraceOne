using Microsoft.EntityFrameworkCore;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Repositories;

internal sealed class AccountActivationTokenRepository(IdentityDbContext context)
    : IAccountActivationTokenRepository
{
    public Task<AccountActivationToken?> FindByHashAsync(string tokenHash, CancellationToken ct)
        => context.AccountActivationTokens
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, ct);

    public async Task DeleteByUserIdAsync(UserId userId, CancellationToken ct)
    {
        var existing = await context.AccountActivationTokens
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);
        context.AccountActivationTokens.RemoveRange(existing);
    }

    public void Add(AccountActivationToken token)
        => context.AccountActivationTokens.Add(token);
}
