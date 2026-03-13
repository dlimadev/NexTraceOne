using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Application.Features.ActivateUser;
using NexTraceOne.Identity.Application.Features.AssignRole;
using NexTraceOne.Identity.Application.Features.ChangePassword;
using NexTraceOne.Identity.Application.Features.CreateDelegation;
using NexTraceOne.Identity.Application.Features.CreateUser;
using NexTraceOne.Identity.Application.Features.DeactivateUser;
using NexTraceOne.Identity.Application.Features.DecideJitAccess;
using NexTraceOne.Identity.Application.Features.FederatedLogin;
using NexTraceOne.Identity.Application.Features.GetUserProfile;
using NexTraceOne.Identity.Application.Features.ListActiveSessions;
using NexTraceOne.Identity.Application.Features.ListTenantUsers;
using NexTraceOne.Identity.Application.Features.LocalLogin;
using NexTraceOne.Identity.Application.Features.RefreshToken;
using NexTraceOne.Identity.Application.Features.RequestBreakGlass;
using NexTraceOne.Identity.Application.Features.RequestJitAccess;
using NexTraceOne.Identity.Application.Features.RevokeBreakGlass;
using NexTraceOne.Identity.Application.Features.RevokeDelegation;
using NexTraceOne.Identity.Application.Features.RevokeSession;
using NexTraceOne.Identity.Application.Features.SelectTenant;
using NexTraceOne.Identity.Application.Features.StartOidcLogin;
using NexTraceOne.Identity.Application.Features.OidcCallback;
using NexTraceOne.Identity.Application.Features.StartAccessReviewCampaign;
using NexTraceOne.Identity.Application.Features.GetAccessReviewCampaign;
using NexTraceOne.Identity.Application.Features.DecideAccessReviewItem;

namespace NexTraceOne.Identity.Application;

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

        return services;
    }
}
