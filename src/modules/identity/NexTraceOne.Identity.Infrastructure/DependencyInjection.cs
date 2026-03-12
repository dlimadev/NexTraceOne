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
/// Inclui: DbContext com RLS e auditoria, repositórios, serviços de autenticação,
/// hash de senha e contrato público do módulo.
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

        // Repositórios — v1.0 Core
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ITenantMembershipRepository, TenantMembershipRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();

        // Repositórios — v1.1 Enterprise
        services.AddScoped<IBreakGlassRepository, BreakGlassRepository>();
        services.AddScoped<IJitAccessRepository, JitAccessRepository>();
        services.AddScoped<IDelegationRepository, DelegationRepository>();
        services.AddScoped<IAccessReviewRepository, AccessReviewRepository>();
        services.AddScoped<ISecurityEventRepository, SecurityEventRepository>();

        // Repositórios — v1.2 Autorização por Ambiente
        services.AddScoped<IEnvironmentRepository, EnvironmentRepository>();

        // Serviços de autenticação e segurança
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // Provider OIDC para fluxo federado (Authorization Code flow)
        services.AddHttpClient("oidc");
        services.AddScoped<IOidcProvider, OidcProviderService>();

        // Ponte de auditoria: propaga SecurityEvents do Identity para o módulo Audit central.
        // ISecurityAuditBridge é injetado opcionalmente nos handlers que geram eventos críticos.
        services.AddScoped<ISecurityAuditBridge, SecurityAuditBridge>();

        // Contrato público do módulo para consumo por outros módulos
        services.AddScoped<IIdentityModule, IdentityModuleService>();

        return services;
    }
}
