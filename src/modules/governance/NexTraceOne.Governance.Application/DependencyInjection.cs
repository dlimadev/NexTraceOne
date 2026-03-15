using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;

namespace NexTraceOne.Governance.Application;

/// <summary>
/// Registra serviços da camada Application do módulo Governance.
/// Inclui: MediatR handlers, FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços da camada Application do módulo Governance ao contêiner de DI.</summary>
    public static IServiceCollection AddGovernanceApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        return services;
    }
}
