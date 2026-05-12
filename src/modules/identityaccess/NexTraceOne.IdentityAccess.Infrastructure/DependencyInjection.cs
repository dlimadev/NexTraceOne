using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Contracts.ServiceInterfaces;
using NexTraceOne.IdentityAccess.Domain.Events;
using NexTraceOne.IdentityAccess.Infrastructure.Authorization;
using NexTraceOne.IdentityAccess.Infrastructure.Context;
using NexTraceOne.IdentityAccess.Infrastructure.EventHandlers;
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

        // Repositórios — v1.5 Agent-to-Agent Protocol (Wave D.4)
        services.AddScoped<IPlatformApiTokenRepository, EfPlatformApiTokenRepository>();
        services.AddScoped<IAgentQueryRepository, EfAgentQueryRepository>();

        // Repositórios — v1.6 Policy Studio (Wave D.3)
        services.AddScoped<IPolicyDefinitionRepository, PolicyDefinitionRepository>();

        // Repositórios — v2.0 SaaS Evolution (licensing, agent heartbeat, alerts)
        services.AddScoped<ITenantLicenseRepository, EfTenantLicenseRepository>();
        services.AddScoped<IAgentRegistrationRepository, EfAgentRegistrationRepository>();
        services.AddScoped<IAlertFiringRecordRepository, EfAlertFiringRecordRepository>();

        // Repositórios — v2.1 Token Infrastructure
        services.AddScoped<IAccountActivationTokenRepository, AccountActivationTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        // Repositórios — W5-05 Fine-Grained Auth per Environment
        services.AddScoped<IEnvironmentAccessPolicyRepository, EnvironmentAccessPolicyRepository>();
        services.AddScoped<IJitAccessRequestRepository, JitAccessRequestRepository>();

        // Repositórios — SaaS-06 Onboarding Wizard
        services.AddScoped<IOnboardingProgressRepository, OnboardingProgressRepository>();

        // Notifier — usa implementação real via Notifications module quando Smtp:Host está configurado;
        // caso contrário usa NullIdentityNotifier (que regista o token em Warning para dev local).
        var smtpHost = configuration["Smtp:Host"];
        if (!string.IsNullOrWhiteSpace(smtpHost))
            services.AddScoped<IIdentityNotifier>(sp =>
                new NotificationsIdentityNotifier(
                    sp.GetRequiredService<INotificationModule>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<NotificationsIdentityNotifier>>()));
        else
            services.AddScoped<IIdentityNotifier, NullIdentityNotifier>();

        // SaaS-01: Capability resolver para claims JWT
        services.AddScoped<ICapabilityResolver, DefaultCapabilityResolver>();

        // SaaS-08: Alert evaluation background job
        services.AddHostedService<Jobs.AlertEvaluationJob>();

        // Serviços de autenticação e segurança
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IClaimsTransformation, RolePermissionsClaimsTransformation>();
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
        services.AddHttpClient("oidc")
            .AddStandardResilienceHandler();
        services.AddScoped<IOidcProvider, OidcProviderService>();

        // SAML 2.0 SSO — protocolo e provider de configuração
        services.AddScoped<ISamlService, SamlService>();
        services.AddScoped<ISamlConfigProvider, ConfigurationSamlConfigProvider>();

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

        // Leitura de padrões de acesso para detecção de anomalias (Wave AD.3)
        services.AddScoped<IAccessPatternReader, AccessPatternReader>();

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

        // ── Domain Event Handlers (via Outbox → IEventBus) ────────────────────────────
        services.AddScoped<IIntegrationEventHandler<UserCreatedDomainEvent>, UserCreatedDomainEventHandler>();
        services.AddScoped<IIntegrationEventHandler<UserLockedDomainEvent>, UserLockedDomainEventHandler>();

        // ── Wave AC.2 — Developer Activity Report (null reader) ───────────
        services.AddScoped<NexTraceOne.IdentityAccess.Application.Abstractions.IDeveloperActivityReader, NexTraceOne.IdentityAccess.Application.Services.NullDeveloperActivityReader>();

        return services;
    }
}
