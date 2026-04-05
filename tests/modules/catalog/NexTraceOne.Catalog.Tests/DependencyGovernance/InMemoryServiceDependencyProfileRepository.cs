using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.Tests.DependencyGovernance;

/// <summary>
/// Implementação in-memory do IServiceDependencyProfileRepository para testes unitários.
/// </summary>
internal sealed class InMemoryServiceDependencyProfileRepository : IServiceDependencyProfileRepository
{
    private readonly List<ServiceDependencyProfile> _profiles;

    public InMemoryServiceDependencyProfileRepository(IEnumerable<ServiceDependencyProfile>? seed = null)
    {
        _profiles = seed?.ToList() ?? new List<ServiceDependencyProfile>();
    }

    public Task<ServiceDependencyProfile?> FindByServiceIdAsync(Guid serviceId, CancellationToken ct)
        => Task.FromResult(_profiles.FirstOrDefault(p => p.ServiceId == serviceId));

    public Task<IReadOnlyList<ServiceDependencyProfile>> ListWithVulnerabilitiesAsync(
        VulnerabilitySeverity minSeverity, CancellationToken ct)
    {
        var result = _profiles
            .Where(p => p.Dependencies.Any(d => d.Vulnerabilities.Any(v => v.Severity >= minSeverity)))
            .ToList();
        return Task.FromResult<IReadOnlyList<ServiceDependencyProfile>>(result);
    }

    public Task<IReadOnlyList<ServiceDependencyProfile>> ListByTemplateIdAsync(Guid templateId, CancellationToken ct)
    {
        var result = _profiles.Where(p => p.TemplateId == templateId).ToList();
        return Task.FromResult<IReadOnlyList<ServiceDependencyProfile>>(result);
    }

    public Task AddAsync(ServiceDependencyProfile profile, CancellationToken ct)
    {
        _profiles.Add(profile);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ServiceDependencyProfile profile, CancellationToken ct)
    {
        var idx = _profiles.FindIndex(p => p.Id == profile.Id);
        if (idx >= 0) _profiles[idx] = profile;
        return Task.CompletedTask;
    }
}
