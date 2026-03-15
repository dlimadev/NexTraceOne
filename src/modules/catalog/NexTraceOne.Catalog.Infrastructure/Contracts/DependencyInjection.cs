using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Infrastructure.Persistence;
using NexTraceOne.Contracts.Infrastructure.Persistence.Repositories;

namespace NexTraceOne.Contracts.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo Contracts.
/// Inclui: DbContext, Repositórios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo Contracts ao container DI.</summary>
    public static IServiceCollection AddContractsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetConnectionString("ContractsDatabase")
            ?? configuration.GetConnectionString("NexTraceOne")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=nextraceone;Username=postgres;Password=postgres";

        services.AddDbContext<ContractsDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ContractsDbContext>());
        services.AddScoped<IContractVersionRepository, ContractVersionRepository>();
        services.AddScoped<IContractDraftRepository, ContractDraftRepository>();
        services.AddScoped<IContractReviewRepository, ContractReviewRepository>();

        return services;
    }
}
