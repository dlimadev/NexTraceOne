using Microsoft.AspNetCore.Http;

namespace NexTraceOne.BuildingBlocks.Security.MultiTenancy;

/// <summary>
/// Middleware que resolve o tenant ativo para cada requisição HTTP.
/// Prioridade: 1) JWT claim "tenant_id" 2) Header "X-Tenant-Id" 3) Subdomínio.
/// Requisições sem tenant → 401 (exceto endpoints públicos).
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // TODO: Implementar resolução de tenant
        await next(context);
    }
}
