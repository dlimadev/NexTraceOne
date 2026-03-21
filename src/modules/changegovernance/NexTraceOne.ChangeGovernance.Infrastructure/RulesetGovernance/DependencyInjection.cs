using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Abstractions;
using NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Persistence.Repositories;

namespace NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance;

/// <summary>
/// Registra serviços de infraestrutura do módulo RulesetGovernance.
/// Inclui: DbContext, Repositorios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo RulesetGovernance ao container DI.</summary>
    public static IServiceCollection AddRulesetGovernanceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetConnectionString("RulesetGovernanceDatabase")
            ?? configuration.GetConnectionString("NexTraceOne")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'RulesetGovernanceDatabase' (or fallback 'NexTraceOne'/'DefaultConnection') is not configured.");

        services.AddDbContext<RulesetGovernanceDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<RulesetGovernanceDbContext>());
        services.AddScoped<IRulesetRepository, RulesetRepository>();
        services.AddScoped<IRulesetBindingRepository, RulesetBindingRepository>();
        services.AddScoped<ILintResultRepository, LintResultRepository>();

        return services;
    }
}
