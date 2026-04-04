using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.Catalog.Application.Templates;
using NexTraceOne.Catalog.Infrastructure.Templates;

namespace NexTraceOne.Catalog.API.Templates;

/// <summary>
/// Registra todos os serviços do módulo Service Templates &amp; Scaffolding (Phase 3.1).
/// Compõe Application Layer (handlers + validators) + Infrastructure Layer (DbContext + repositório).
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona o módulo de Templates ao container DI.</summary>
    public static IServiceCollection AddCatalogTemplatesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCatalogTemplatesApplication(configuration);
        services.AddCatalogTemplatesInfrastructure(configuration);
        return services;
    }
}
