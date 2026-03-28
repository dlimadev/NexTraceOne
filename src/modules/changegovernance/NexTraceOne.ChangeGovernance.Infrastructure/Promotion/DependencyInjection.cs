using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;
using NexTraceOne.ChangeGovernance.Contracts.Promotion.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence.Repositories;
using NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Services;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Promotion;

/// <summary>
/// Registra serviços de infraestrutura do módulo Promotion.
/// Inclui: DbContext, Repositórios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo Promotion ao container DI.</summary>
    public static IServiceCollection AddPromotionInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("PromotionDatabase", "NexTraceOne");

        services.AddDbContext<PromotionDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PromotionDbContext>());
        services.AddScoped<IDeploymentEnvironmentRepository, DeploymentEnvironmentRepository>();
        services.AddScoped<IPromotionRequestRepository, PromotionRequestRepository>();
        services.AddScoped<IPromotionGateRepository, PromotionGateRepository>();
        services.AddScoped<IGateEvaluationRepository, GateEvaluationRepository>();

        // Cross-module public interface — outros módulos consomem IPromotionModule
        services.AddScoped<IPromotionModule, PromotionModuleService>();

        return services;
    }
}
