using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Contracts.ServiceInterfaces;
using NexTraceOne.Licensing.Infrastructure.Persistence;
using NexTraceOne.Licensing.Infrastructure.Persistence.Repositories;
using NexTraceOne.Licensing.Infrastructure.Services;

namespace NexTraceOne.Licensing.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo Licensing.
/// Inclui: DbContext, Repositórios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddLicensingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetConnectionString("LicensingDatabase")
            ?? configuration.GetConnectionString("NexTraceOne")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=nextraceone;Username=postgres;Password=postgres";

        services.AddDbContext<LicensingDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<LicensingDbContext>());
        services.AddScoped<ILicenseRepository, LicenseRepository>();
        services.AddScoped<IHardwareBindingRepository, HardwareBindingRepository>();
        services.AddScoped<IHardwareFingerprintProvider, HardwareFingerprintProvider>();
        services.AddScoped<ILicensingModule, LicensingModuleService>();

        return services;
    }
}
