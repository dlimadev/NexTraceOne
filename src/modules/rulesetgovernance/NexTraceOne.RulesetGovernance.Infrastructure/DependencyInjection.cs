using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.RulesetGovernance.Application.Abstractions;
using NexTraceOne.RulesetGovernance.Infrastructure.Persistence;
using NexTraceOne.RulesetGovernance.Infrastructure.Persistence.Repositories;

namespace NexTraceOne.RulesetGovernance.Infrastructure;

/// <summary>
/// Registra servicos de infraestrutura do modulo RulesetGovernance.
/// Inclui: DbContext, Repositorios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os servicos de infraestrutura do modulo RulesetGovernance ao container DI.</summary>
    public static IServiceCollection AddRulesetGovernanceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetConnectionString("RulesetGovernanceDatabase")
            ?? configuration.GetConnectionString("NexTraceOne")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=nextraceone;Username=postgres;Password=postgres";

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
