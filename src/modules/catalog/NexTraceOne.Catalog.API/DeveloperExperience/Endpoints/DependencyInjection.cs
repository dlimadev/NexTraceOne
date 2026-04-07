using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.Catalog.Application.DeveloperExperience;
using NexTraceOne.Catalog.Infrastructure.DeveloperExperience;

namespace NexTraceOne.Catalog.API.DeveloperExperience.Endpoints;

/// <summary>
/// Registra serviços específicos da camada API do módulo DeveloperExperience.
/// Compõe Application + Infrastructure layers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddDeveloperExperienceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDeveloperExperienceApplication(configuration);
        services.AddDeveloperExperienceInfrastructure(configuration);
        return services;
    }
}
