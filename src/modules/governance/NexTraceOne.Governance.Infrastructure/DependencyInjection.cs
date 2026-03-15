using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.Governance.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo Governance.
/// Fase atual: sem persistência própria — agrega dados de outros módulos.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo Governance.</summary>
    public static IServiceCollection AddGovernanceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services;
    }
}
