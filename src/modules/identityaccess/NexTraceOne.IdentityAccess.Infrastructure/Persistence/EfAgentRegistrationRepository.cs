using Microsoft.EntityFrameworkCore;

using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence;

internal sealed class EfAgentRegistrationRepository(IdentityDbContext context) : IAgentRegistrationRepository
{
    public async Task<AgentRegistration?> GetByHostUnitIdAsync(Guid tenantId, Guid hostUnitId, CancellationToken ct = default)
        => await context.AgentRegistrations
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.HostUnitId == hostUnitId, ct);

    public async Task<IReadOnlyList<AgentRegistration>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await context.AgentRegistrations
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.LastHeartbeatAt)
            .ToListAsync(ct);

    public async Task<decimal> SumActiveHostUnitsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var result = await context.AgentRegistrations
            .Where(a => a.TenantId == tenantId && a.Status == AgentRegistrationStatus.Active)
            .SumAsync(a => (decimal?)a.HostUnits, ct);
        return result ?? 0m;
    }

    public void Add(AgentRegistration registration) => context.AgentRegistrations.Add(registration);

    public void Update(AgentRegistration registration) => context.AgentRegistrations.Update(registration);
}
