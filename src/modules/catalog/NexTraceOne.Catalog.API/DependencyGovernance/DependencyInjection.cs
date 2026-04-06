using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.Catalog.Application.DependencyGovernance;
using NexTraceOne.Catalog.Infrastructure.DependencyGovernance;

namespace NexTraceOne.Catalog.API.DependencyGovernance;

/// <summary>
/// Registra todos os serviços do módulo Dependency Governance.
/// Compõe Application Layer + Infrastructure Layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCatalogDependencyGovernanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCatalogDependencyGovernanceApplication(configuration);
        services.AddCatalogDependencyGovernanceInfrastructure(configuration);
        return services;
    }
}
