using Microsoft.AspNetCore.Builder;

namespace NexTraceOne.BuildingBlocks.Security.Session;

/// <summary>
/// Extensões para registo do middleware de segurança de sessão.
/// </summary>
public static class SessionSecurityExtensions
{
    /// <summary>
    /// Adiciona o middleware de inactividade e validação de sessão ao pipeline HTTP.
    /// Deve ser chamado após UseAuthentication e antes de UseAuthorization.
    /// </summary>
    public static IApplicationBuilder UseSessionSecurity(this IApplicationBuilder app)
        => app.UseMiddleware<SessionInactivityMiddleware>();
}
