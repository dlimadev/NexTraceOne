using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.CostIntelligence.Application.Abstractions;
using NexTraceOne.CostIntelligence.Infrastructure.Persistence;
using NexTraceOne.CostIntelligence.Infrastructure.Persistence.Repositories;

namespace NexTraceOne.CostIntelligence.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo CostIntelligence.
/// Inclui: DbContext com connection string isolada, repositórios e UnitOfWork.
/// Cada módulo possui sua própria base de dados — sem compartilhamento.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo CostIntelligence ao container DI.</summary>
    public static IServiceCollection AddCostIntelligenceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetConnectionString("CostIntelligenceDatabase")
            ?? configuration.GetConnectionString("NexTraceOne")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=nextraceone;Username=postgres;Password=postgres";

        services.AddDbContext<CostIntelligenceDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CostIntelligenceDbContext>());
        services.AddScoped<ICostSnapshotRepository, CostSnapshotRepository>();
        services.AddScoped<ICostAttributionRepository, CostAttributionRepository>();
        services.AddScoped<IServiceCostProfileRepository, ServiceCostProfileRepository>();

        return services;
    }
}
