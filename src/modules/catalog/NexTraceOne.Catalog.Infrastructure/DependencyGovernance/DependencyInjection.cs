using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Catalog.Application.DependencyGovernance.Abstractions;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Infrastructure.DependencyGovernance.External;
using NexTraceOne.Catalog.Infrastructure.DependencyGovernance.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.DependencyGovernance;

/// <summary>
/// Registra serviços de infraestrutura do módulo Dependency Governance.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCatalogDependencyGovernanceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("CatalogDatabase", "NexTraceOne");

        services.AddDbContext<DependencyGovernanceDbContext>((sp, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DependencyGovernanceDbContext>());
        services.AddScoped<IDependencyGovernanceUnitOfWork>(sp => sp.GetRequiredService<DependencyGovernanceDbContext>());
        services.AddScoped<IServiceDependencyProfileRepository, ServiceDependencyProfileRepository>();
        services.AddScoped<IVulnerabilityAdvisoryRepository, EfVulnerabilityAdvisoryRepository>();

        // ── External data sources ────────────────────────────────────
        services.AddHttpClient<OSVVulnerabilityClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.osv.dev/v1/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<IVulnerabilityDataSource>(sp =>
            sp.GetRequiredService<OSVVulnerabilityClient>());

        services.AddHttpClient<NuGetPackageClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.nuget.org/v3/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddSingleton<IPackageMetadataClient>(sp =>
            sp.GetRequiredService<NuGetPackageClient>());

        services.AddScoped<IDependencyEnrichmentService, DependencyEnrichmentService>();

        services.AddHttpClient<ILlmCompletionClient, OllamaCompletionClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:11434");
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }
}
