using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Contracts.ServiceInterfaces;
using NexTraceOne.AuditCompliance.Infrastructure.Persistence;
using NexTraceOne.AuditCompliance.Infrastructure.Persistence.Repositories;
using NexTraceOne.AuditCompliance.Infrastructure.Services;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;

namespace NexTraceOne.AuditCompliance.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo Audit.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAuditInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetConnectionString("AuditDatabase")
            ?? configuration.GetConnectionString("NexTraceOne")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'AuditDatabase' (or fallback 'NexTraceOne'/'DefaultConnection') is not configured.");

        services.AddDbContext<AuditDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AuditDbContext>());
        services.AddScoped<IAuditEventRepository, AuditEventRepository>();
        services.AddScoped<IAuditChainRepository, AuditChainRepository>();
        services.AddScoped<IAuditModule, AuditModuleService>();

        return services;
    }
}
