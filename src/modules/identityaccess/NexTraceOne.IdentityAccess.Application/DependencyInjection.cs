using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.ActivateUser;
using NexTraceOne.IdentityAccess.Application.Features.AssignRole;
using NexTraceOne.IdentityAccess.Application.Features.ChangePassword;
using NexTraceOne.IdentityAccess.Application.Features.CreateDelegation;
using NexTraceOne.IdentityAccess.Application.Features.CreateUser;
using NexTraceOne.IdentityAccess.Application.Features.DeactivateUser;
using NexTraceOne.IdentityAccess.Application.Features.DecideAccessReviewItem;
using NexTraceOne.IdentityAccess.Application.Features.DecideJitAccess;
using NexTraceOne.IdentityAccess.Application.Features.FederatedLogin;
using NexTraceOne.IdentityAccess.Application.Features.GetAccessReviewCampaign;
using NexTraceOne.IdentityAccess.Application.Features.GetUserProfile;
using NexTraceOne.IdentityAccess.Application.Features.ListActiveSessions;
using NexTraceOne.IdentityAccess.Application.Features.ListSecurityEvents;
using NexTraceOne.IdentityAccess.Application.Features.ListTenantUsers;
using NexTraceOne.IdentityAccess.Application.Features.LocalLogin;
using NexTraceOne.IdentityAccess.Application.Features.OidcCallback;
using NexTraceOne.IdentityAccess.Application.Features.RefreshToken;
using NexTraceOne.IdentityAccess.Application.Features.RequestBreakGlass;
using NexTraceOne.IdentityAccess.Application.Features.RequestJitAccess;
using NexTraceOne.IdentityAccess.Application.Features.RevokeBreakGlass;
using NexTraceOne.IdentityAccess.Application.Features.RevokeDelegation;
using NexTraceOne.IdentityAccess.Application.Features.RevokeSession;
using NexTraceOne.IdentityAccess.Application.Features.SelectTenant;
using NexTraceOne.IdentityAccess.Application.Features.StartAccessReviewCampaign;
using NexTraceOne.IdentityAccess.Application.Features.StartOidcLogin;
using NexTraceOne.IdentityAccess.Application.Features.VerifyMfaChallenge;
using NexTraceOne.IdentityAccess.Application.Features.GetDeveloperActivityReport;

namespace NexTraceOne.IdentityAccess.Application;

/// <summary>
/// Registra serviços da camada Application do módulo Identity.
/// Inclui: MediatR handlers (assembly scanning), FluentValidation validators (explícito).
/// Cada novo feature com Validator deve ser registrado aqui.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Serviços de aplicação extraídos dos handlers para aderir ao DIP/SRP
        services.AddScoped<ISecurityAuditRecorder, Features.SecurityAuditRecorder>();
        services.AddScoped<ILoginSessionCreator, Features.LoginSessionCreator>();
        services.AddScoped<ILoginResponseBuilder, Features.LoginResponseBuilder>();

        // Autenticação
        services.AddTransient<IValidator<LocalLogin.Command>, LocalLogin.Validator>();
        services.AddTransient<IValidator<FederatedLogin.Command>, FederatedLogin.Validator>();
        services.AddTransient<IValidator<RefreshToken.Command>, RefreshToken.Validator>();
        services.AddTransient<IValidator<RevokeSession.Command>, RevokeSession.Validator>();
        services.AddTransient<IValidator<ChangePassword.Command>, ChangePassword.Validator>();
        services.AddTransient<IValidator<VerifyMfaChallenge.Command>, VerifyMfaChallenge.Validator>();

        // OIDC Redirect Flow
        services.AddTransient<IValidator<StartOidcLogin.Command>, StartOidcLogin.Validator>();
        services.AddTransient<IValidator<OidcCallback.Command>, OidcCallback.Validator>();

        // Gestão de usuários
        services.AddTransient<IValidator<CreateUser.Command>, CreateUser.Validator>();
        services.AddTransient<IValidator<GetUserProfile.Query>, GetUserProfile.Validator>();
        services.AddTransient<IValidator<ListTenantUsers.Query>, ListTenantUsers.Validator>();
        services.AddTransient<IValidator<DeactivateUser.Command>, DeactivateUser.Validator>();
        services.AddTransient<IValidator<ActivateUser.Command>, ActivateUser.Validator>();
        services.AddTransient<IValidator<ListActiveSessions.Query>, ListActiveSessions.Validator>();
        services.AddTransient<IValidator<AssignRole.Command>, AssignRole.Validator>();

        // Enterprise — Break Glass, JIT Access, Delegação
        services.AddTransient<IValidator<RequestBreakGlass.Command>, RequestBreakGlass.Validator>();
        services.AddTransient<IValidator<RevokeBreakGlass.Command>, RevokeBreakGlass.Validator>();
        services.AddTransient<IValidator<RequestJitAccess.Command>, RequestJitAccess.Validator>();
        services.AddTransient<IValidator<DecideJitAccess.Command>, DecideJitAccess.Validator>();
        services.AddTransient<IValidator<CreateDelegation.Command>, CreateDelegation.Validator>();
        services.AddTransient<IValidator<RevokeDelegation.Command>, RevokeDelegation.Validator>();
        services.AddTransient<IValidator<SelectTenant.Command>, SelectTenant.Validator>();

        // Access Review — recertificação periódica de acessos
        services.AddTransient<IValidator<StartAccessReviewCampaign.Command>, StartAccessReviewCampaign.Validator>();
        services.AddTransient<IValidator<GetAccessReviewCampaign.Query>, GetAccessReviewCampaign.Validator>();
        services.AddTransient<IValidator<DecideAccessReviewItem.Command>, DecideAccessReviewItem.Validator>();

        // Segurança
        services.AddTransient<IValidator<ListSecurityEvents.Query>, ListSecurityEvents.Validator>();

        // ── Wave AC.2 — Developer Activity Report ─────────────────────────
        services.AddTransient<IValidator<GetDeveloperActivityReport.Query>, GetDeveloperActivityReport.Validator>();

        return services;
    }
}
