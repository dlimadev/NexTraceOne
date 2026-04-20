using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NexTraceOne.BuildingBlocks.Security.CookieSession;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

/// <summary>
/// Orquestrador de endpoints Minimal API do módulo Identity.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// Delega para ficheiros especializados por domínio funcional:
/// Auth, Users, RolePermission, BreakGlass, JitAccess, Delegation, Tenant, AccessReview e Environment.
///
/// Endpoints de sessão cookie (CookieSessionEndpoints) são registados condicionalmente
/// quando Auth:CookieSession:Enabled = true (rollout controlado — desabilitado por padrão).
/// </summary>
public sealed class IdentityEndpointModule
{
    /// <summary>
    /// Ponto de entrada do assembly scanning — regista todos os sub-módulos de endpoints.
    /// </summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/identity");

        AuthEndpoints.Map(group);
        UserEndpoints.Map(group);
        RolePermissionEndpoints.Map(group);
        BreakGlassEndpoints.Map(group);
        JitAccessEndpoints.Map(group);
        DelegationEndpoints.Map(group);
        TenantEndpoints.Map(group);
        AccessReviewEndpoints.Map(group);
        EnvironmentEndpoints.Map(group);
        RuntimeContextEndpoints.Map(group);
        SecurityEventsEndpoints.Map(group);

        // Endpoints de sessão cookie — apenas quando feature flag ativa
        var cookieSessionOptions = app.ServiceProvider
            .GetRequiredService<IOptions<CookieSessionOptions>>().Value;

        if (cookieSessionOptions.Enabled)
        {
            CookieSessionEndpoints.Map(group);
        }
    }
}
