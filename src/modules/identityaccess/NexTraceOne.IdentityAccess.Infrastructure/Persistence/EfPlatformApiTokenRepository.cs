using Microsoft.EntityFrameworkCore;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence;

internal sealed class EfPlatformApiTokenRepository(IdentityDbContext db)
    : IPlatformApiTokenRepository
{
    public async Task<PlatformApiToken?> GetByIdAsync(PlatformApiTokenId id, CancellationToken ct)
        => await db.PlatformApiTokens.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<PlatformApiToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct)
        => await db.PlatformApiTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<IReadOnlyList<PlatformApiToken>> ListByTenantAsync(Guid tenantId, CancellationToken ct)
        => await db.PlatformApiTokens
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(PlatformApiToken token, CancellationToken ct)
        => await db.PlatformApiTokens.AddAsync(token, ct);

    public Task UpdateAsync(PlatformApiToken token, CancellationToken ct)
    {
        db.PlatformApiTokens.Update(token);
        return Task.CompletedTask;
    }
}
