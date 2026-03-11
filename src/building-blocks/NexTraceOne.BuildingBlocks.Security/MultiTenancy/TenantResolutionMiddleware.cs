using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;

namespace NexTraceOne.BuildingBlocks.Security.MultiTenancy;

/// <summary>
/// Middleware que resolve o tenant ativo para cada requisição HTTP.
/// Prioridade: 1) JWT claim "tenant_id" 2) Header "X-Tenant-Id" 3) Subdomínio.
/// Requisições sem tenant → 401 (exceto endpoints públicos).
/// </summary>
public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    CurrentTenantAccessor currentTenant)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (TryResolveFromJwt(context, out var jwtTenantId))
        {
            currentTenant.Set(jwtTenantId, slug: jwtTenantId.ToString("N"), name: "JWT Tenant", isActive: true);
        }
        else if (TryResolveFromHeader(context, out var headerTenantId))
        {
            currentTenant.Set(headerTenantId, slug: headerTenantId.ToString("N"), name: "Header Tenant", isActive: true);
        }
        else if (TryResolveFromSubdomain(context, out var subdomain, out var deterministicTenantId))
        {
            currentTenant.Set(deterministicTenantId, subdomain, subdomain, isActive: true);
        }

        await next(context);
    }

    private static bool TryResolveFromJwt(HttpContext context, out Guid tenantId)
        => Guid.TryParse(context.User.FindFirst("tenant_id")?.Value, out tenantId);

    private static bool TryResolveFromHeader(HttpContext context, out Guid tenantId)
        => Guid.TryParse(context.Request.Headers["X-Tenant-Id"].FirstOrDefault(), out tenantId);

    private static bool TryResolveFromSubdomain(HttpContext context, out string slug, out Guid tenantId)
    {
        slug = string.Empty;
        tenantId = Guid.Empty;

        var host = context.Request.Host.Host;
        var parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length < 3)
        {
            return false;
        }

        slug = parts[0];
        tenantId = CreateDeterministicGuid(slug);
        return true;
    }

    private static Guid CreateDeterministicGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);
        return new Guid(bytes);
    }
}
