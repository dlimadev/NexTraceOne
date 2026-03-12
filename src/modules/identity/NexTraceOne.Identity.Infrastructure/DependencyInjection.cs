using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Contracts.ServiceInterfaces;
using NexTraceOne.Identity.Infrastructure.Persistence;
using NexTraceOne.Identity.Infrastructure.Persistence.Repositories;
using NexTraceOne.Identity.Infrastructure.Services;

namespace NexTraceOne.Identity.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo Identity.
/// Inclui: DbContext, Repositórios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetConnectionString("IdentityDatabase")
            ?? configuration.GetConnectionString("NexTraceOne")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=nextraceone;Username=postgres;Password=postgres";

        services.AddDbContext<IdentityDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ITenantMembershipRepository, TenantMembershipRepository>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IIdentityModule, IdentityModuleService>();

        return services;
    }
}
