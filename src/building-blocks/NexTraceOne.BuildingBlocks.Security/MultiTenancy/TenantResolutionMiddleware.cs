using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace NexTraceOne.BuildingBlocks.Security.MultiTenancy;

/// <summary>
/// Middleware que resolve o tenant ativo para cada requisição HTTP.
///
/// Prioridade de resolução:
/// 1) JWT claim "tenant_id" — fonte de verdade após autenticação.
/// 2) Header "X-Tenant-Id" — usado antes do login estar completo (ex: seleção de tenant).
/// 3) Subdomínio — resolução por convenção de host.
///
/// Segurança:
/// - O header X-Tenant-Id é aceito apenas como fallback quando JWT não está presente.
///   O backend DEVE validar que o tenant resolvido pertence ao usuário autenticado
///   nos behaviors de pipeline (TenantIsolationBehavior).
/// - Subdomínios geram IDs determinísticos via SHA-256 — previsíveis, mas validados
///   no pipeline antes do acesso a dados.
/// - Requisições sem tenant → processadas sem contexto de tenant.
///   Endpoints que exigem tenant são bloqueados pelo TenantIsolationBehavior.
/// </summary>
public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    ILogger<TenantResolutionMiddleware> logger)
{
    /// <summary>
    /// Resolve o tenant ativo para cada requisição HTTP.
    /// CurrentTenantAccessor é scoped e deve ser injetado via parâmetro do InvokeAsync,
    /// pois o middleware é singleton e não pode receber dependências scoped no construtor.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, CurrentTenantAccessor currentTenant)
    {
        if (TryResolveFromJwt(context, out var jwtTenantId))
        {
            currentTenant.Set(jwtTenantId, slug: jwtTenantId.ToString("N"), name: "JWT Tenant", isActive: true);
            logger.LogDebug("Tenant resolved from JWT claim: {TenantId}", jwtTenantId);
        }
        else if (TryResolveFromHeader(context, out var headerTenantId))
        {
            // Segurança: o header é aceito como fallback, mas sem JWT o request só terá
            // acesso a endpoints públicos. O TenantIsolationBehavior impede acesso a dados
            // sem autenticação adequada. Registrar para fins de auditoria e troubleshooting.
            currentTenant.Set(headerTenantId, slug: headerTenantId.ToString("N"), name: "Header Tenant", isActive: true);
            logger.LogDebug("Tenant resolved from X-Tenant-Id header: {TenantId}", headerTenantId);
        }
        else if (TryResolveFromSubdomain(context, out var subdomain, out var deterministicTenantId))
        {
            currentTenant.Set(deterministicTenantId, subdomain, subdomain, isActive: true);
            logger.LogDebug("Tenant resolved from subdomain '{Subdomain}': {TenantId}", subdomain, deterministicTenantId);
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
