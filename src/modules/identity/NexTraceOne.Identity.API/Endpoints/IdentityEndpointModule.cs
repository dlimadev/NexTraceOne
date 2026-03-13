using Microsoft.AspNetCore.Builder;

namespace NexTraceOne.Identity.API.Endpoints;

/// <summary>
/// Orquestrador de endpoints Minimal API do módulo Identity.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// Delega para ficheiros especializados por domínio funcional:
/// Auth, Users, RolePermission, BreakGlass, JitAccess, Delegation, Tenant e AccessReview.
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
    }
}
