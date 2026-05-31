using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;

namespace NexTraceOne.Catalog.Application.DependencyGovernance.Abstractions;

/// <summary>
/// Serviço que enriquece um perfil de dependências com dados de registries públicos.
/// </summary>
public interface IDependencyEnrichmentService
{
    Task EnrichAsync(ServiceDependencyProfile profile, CancellationToken cancellationToken = default);
}
