using Microsoft.EntityFrameworkCore;

using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence;

internal sealed class EfTenantLicenseRepository(IdentityDbContext context) : ITenantLicenseRepository
{
    public async Task<TenantLicense?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
        => await context.TenantLicenses
            .FirstOrDefaultAsync(l => l.TenantId == tenantId, ct);

    public async Task<TenantLicense?> GetByIdAsync(TenantLicenseId id, CancellationToken ct = default)
        => await context.TenantLicenses.FindAsync([id], ct);

    public async Task<IReadOnlyList<TenantLicense>> ListAllAsync(CancellationToken ct = default)
        => await context.TenantLicenses.ToListAsync(ct);

    public void Add(TenantLicense license) => context.TenantLicenses.Add(license);

    public void Update(TenantLicense license) => context.TenantLicenses.Update(license);
}
