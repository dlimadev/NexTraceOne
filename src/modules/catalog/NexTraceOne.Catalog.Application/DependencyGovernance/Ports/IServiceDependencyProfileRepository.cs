using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.Application.DependencyGovernance.Ports;

public interface IServiceDependencyProfileRepository
{
    Task<ServiceDependencyProfile?> FindByServiceIdAsync(Guid serviceId, CancellationToken ct);
    Task<IReadOnlyList<ServiceDependencyProfile>> ListWithVulnerabilitiesAsync(VulnerabilitySeverity minSeverity, CancellationToken ct);
    Task<IReadOnlyList<ServiceDependencyProfile>> ListByTemplateIdAsync(Guid templateId, CancellationToken ct);
    Task AddAsync(ServiceDependencyProfile profile, CancellationToken ct);
    Task UpdateAsync(ServiceDependencyProfile profile, CancellationToken ct);
}
