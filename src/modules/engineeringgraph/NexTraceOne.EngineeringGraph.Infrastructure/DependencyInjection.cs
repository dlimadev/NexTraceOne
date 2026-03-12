using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Contracts.ServiceInterfaces;
using NexTraceOne.EngineeringGraph.Infrastructure.Persistence;
using NexTraceOne.EngineeringGraph.Infrastructure.Persistence.Repositories;
using NexTraceOne.EngineeringGraph.Infrastructure.Services;

namespace NexTraceOne.EngineeringGraph.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo EngineeringGraph.
/// Inclui: DbContext, Repositórios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddEngineeringGraphInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetConnectionString("EngineeringGraphDatabase")
            ?? configuration.GetConnectionString("NexTraceOne")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=nextraceone;Username=postgres;Password=postgres";

        services.AddDbContext<EngineeringGraphDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<EngineeringGraphDbContext>());
        services.AddScoped<IApiAssetRepository, ApiAssetRepository>();
        services.AddScoped<IServiceAssetRepository, ServiceAssetRepository>();
        services.AddScoped<IEngineeringGraphModule, EngineeringGraphModuleService>();

        return services;
    }
}
