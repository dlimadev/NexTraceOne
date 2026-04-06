using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.Application.DependencyGovernance.Ports;

public interface IServiceDependencyProfileRepository
{
    Task<ServiceDependencyProfile?> FindByServiceIdAsync(Guid serviceId, CancellationToken ct);
    Task<IReadOnlyList<ServiceDependencyProfile>> ListWithVulnerabilitiesAsync(VulnerabilitySeverity minSeverity, CancellationToken ct);
    Task<IReadOnlyList<ServiceDependencyProfile>> ListByTemplateIdAsync(Guid templateId, CancellationToken ct);
    /// <summary>Lista todos os perfis que incluem uma dependência do pacote especificado.</summary>
    Task<IReadOnlyList<ServiceDependencyProfile>> ListByPackageNameAsync(string packageName, CancellationToken ct);

    Task AddAsync(ServiceDependencyProfile profile, CancellationToken ct);
    Task UpdateAsync(ServiceDependencyProfile profile, CancellationToken ct);
}
