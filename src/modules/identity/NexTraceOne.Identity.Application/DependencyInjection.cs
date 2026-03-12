using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Identity.Application.Features.ActivateUser;
using NexTraceOne.Identity.Application.Features.AssignRole;
using NexTraceOne.Identity.Application.Features.ChangePassword;
using NexTraceOne.Identity.Application.Features.CreateUser;
using NexTraceOne.Identity.Application.Features.DeactivateUser;
using NexTraceOne.Identity.Application.Features.FederatedLogin;
using NexTraceOne.Identity.Application.Features.GetUserProfile;
using NexTraceOne.Identity.Application.Features.ListActiveSessions;
using NexTraceOne.Identity.Application.Features.ListTenantUsers;
using NexTraceOne.Identity.Application.Features.LocalLogin;
using NexTraceOne.Identity.Application.Features.RefreshToken;
using NexTraceOne.Identity.Application.Features.RevokeSession;

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

        // Autenticação
        services.AddTransient<IValidator<LocalLogin.Command>, LocalLogin.Validator>();
        services.AddTransient<IValidator<FederatedLogin.Command>, FederatedLogin.Validator>();
        services.AddTransient<IValidator<RefreshToken.Command>, RefreshToken.Validator>();
        services.AddTransient<IValidator<RevokeSession.Command>, RevokeSession.Validator>();
        services.AddTransient<IValidator<ChangePassword.Command>, ChangePassword.Validator>();

        // Gestão de usuários
        services.AddTransient<IValidator<CreateUser.Command>, CreateUser.Validator>();
        services.AddTransient<IValidator<GetUserProfile.Query>, GetUserProfile.Validator>();
        services.AddTransient<IValidator<ListTenantUsers.Query>, ListTenantUsers.Validator>();
        services.AddTransient<IValidator<DeactivateUser.Command>, DeactivateUser.Validator>();
        services.AddTransient<IValidator<ActivateUser.Command>, ActivateUser.Validator>();
        services.AddTransient<IValidator<ListActiveSessions.Query>, ListActiveSessions.Validator>();
        services.AddTransient<IValidator<AssignRole.Command>, AssignRole.Validator>();

        return services;
    }
}
