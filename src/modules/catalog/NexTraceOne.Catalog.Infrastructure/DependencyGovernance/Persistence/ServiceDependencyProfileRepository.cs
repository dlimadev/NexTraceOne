using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

using Microsoft.EntityFrameworkCore;

namespace NexTraceOne.Catalog.Infrastructure.DependencyGovernance.Persistence;

internal sealed class ServiceDependencyProfileRepository(DependencyGovernanceDbContext context) : IServiceDependencyProfileRepository
{
    public async Task<ServiceDependencyProfile?> FindByServiceIdAsync(Guid serviceId, CancellationToken ct)
        => await context.ServiceDependencyProfiles
            .Include(p => p.Dependencies)
            .FirstOrDefaultAsync(p => p.ServiceId == serviceId, ct);

    public async Task<IReadOnlyList<ServiceDependencyProfile>> ListWithVulnerabilitiesAsync(VulnerabilitySeverity minSeverity, CancellationToken ct)
    {
        var all = await context.ServiceDependencyProfiles
            .Include(p => p.Dependencies)
            .ToListAsync(ct);

        return all.Where(p => p.Dependencies.Any(d =>
            d.Vulnerabilities.Any(v => v.Severity >= minSeverity)))
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<ServiceDependencyProfile>> ListByTemplateIdAsync(Guid templateId, CancellationToken ct)
    {
        return (await context.ServiceDependencyProfiles
            .Include(p => p.Dependencies)
            .Where(p => p.TemplateId == templateId)
            .ToListAsync(ct)).AsReadOnly();
    }

    public async Task AddAsync(ServiceDependencyProfile profile, CancellationToken ct)
    {
        await context.ServiceDependencyProfiles.AddAsync(profile, ct);
    }

    public Task UpdateAsync(ServiceDependencyProfile profile, CancellationToken ct)
    {
        context.ServiceDependencyProfiles.Update(profile);
        return Task.CompletedTask;
    }
}
