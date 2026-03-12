using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Audit.Application.Abstractions;
using NexTraceOne.Audit.Contracts.ServiceInterfaces;
using NexTraceOne.Audit.Infrastructure.Persistence;
using NexTraceOne.Audit.Infrastructure.Persistence.Repositories;
using NexTraceOne.Audit.Infrastructure.Services;

namespace NexTraceOne.Audit.Infrastructure;

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
            ?? "Host=localhost;Database=nextraceone;Username=postgres;Password=postgres";

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
