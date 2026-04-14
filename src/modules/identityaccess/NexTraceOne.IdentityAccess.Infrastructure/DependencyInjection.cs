using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Contracts.ServiceInterfaces;
using NexTraceOne.IdentityAccess.Infrastructure.Authorization;
using NexTraceOne.IdentityAccess.Infrastructure.Context;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence.Repositories;
using NexTraceOne.IdentityAccess.Infrastructure.Services;

namespace NexTraceOne.IdentityAccess.Infrastructure;

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

        var connectionString = configuration.GetRequiredConnectionString("IdentityDatabase", "NexTraceOne");

        services.AddDbContext<IdentityDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());
        services.AddScoped<IIdentityAccessUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());

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
        services.AddScoped<ISsoGroupMappingRepository, SsoGroupMappingRepository>();

        // Repositórios — v1.2 Autorização por Ambiente
        services.AddScoped<IEnvironmentRepository, EnvironmentRepository>();

        // Repositórios — v1.3 Autorização Granular Enterprise
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IModuleAccessPolicyRepository, ModuleAccessPolicyRepository>();

        // Repositórios — v1.4 Multi-Role por Tenant
        services.AddScoped<IUserRoleAssignmentRepository, UserRoleAssignmentRepository>();

        // Serviços de autenticação e segurança
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ITotpVerifier, TotpVerifier>();
        services.AddScoped<IMfaChallengeTokenService, MfaChallengeTokenService>();

        // JIT permission provider — permite ao PermissionAuthorizationHandler verificar grants JIT activos.
        services.AddScoped<IJitPermissionProvider, JitPermissionProvider>();

        // Database permission provider — permite ao PermissionAuthorizationHandler verificar
        // permissões persistidas em base de dados com suporte a personalização por tenant.
        services.AddScoped<IDatabasePermissionProvider, DatabasePermissionProvider>();

        // Module access permission provider — permite ao PermissionAuthorizationHandler e ao
        // ModuleAccessAuthorizationHandler verificar políticas de acesso ao nível de módulo/página/ação,
        // completando a ligação entre o modelo granular (ModuleAccessPolicy) e o pipeline de autorização.
        services.AddScoped<IModuleAccessPermissionProvider, ModuleAccessPermissionProvider>();

        // Provider OIDC para fluxo federado (Authorization Code flow)
        services.AddHttpClient("oidc");
        services.AddScoped<IOidcProvider, OidcProviderService>();

        // Ponte de auditoria: propaga SecurityEvents do Identity para o módulo Audit central.
        // ISecurityAuditBridge é injetado opcionalmente nos handlers que geram eventos críticos.
        services.AddScoped<ISecurityAuditBridge, SecurityAuditBridge>();

        // Rastreador de eventos de segurança para propagação automática ao Audit central.
        // Escopo por requisição — acumula eventos durante a execução do handler.
        services.AddScoped<ISecurityEventTracker, SecurityEventTracker>();

        // Resolução de permissões DB-first com fallback estático
        services.AddScoped<IPermissionResolver, PermissionResolver>();

        // Contrato público do módulo para consumo por outros módulos
        services.AddScoped<IIdentityModule, IdentityModuleService>();

        // Fase 2 — Contexto operacional e resolução de ambiente
        services.AddScoped<EnvironmentContextAccessor>();
        services.AddScoped<IEnvironmentContextAccessor>(sp => sp.GetRequiredService<EnvironmentContextAccessor>());
        services.AddScoped<ITenantEnvironmentContextResolver, TenantEnvironmentContextResolver>();
        services.AddScoped<IEnvironmentProfileResolver, EnvironmentProfileResolver>();
        services.AddScoped<IEnvironmentAccessValidator, EnvironmentAccessValidator>();
        services.AddScoped<IOperationalExecutionContext, OperationalExecutionContext>();
        services.AddScoped<ICurrentEnvironment, CurrentEnvironmentAdapter>();

        // Fase 2 — Authorization handlers
        services.AddScoped<IAuthorizationHandler, EnvironmentAccessAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, OperationalContextAuthorizationHandler>();

        return services;
    }
}
