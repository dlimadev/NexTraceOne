using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Infrastructure.Context;

/// <summary>
/// Middleware que resolve o ambiente ativo para cada requisição HTTP.
///
/// Deve ser registrado APÓS o TenantResolutionMiddleware e APÓS UseAuthentication.
/// Precisa do tenant já resolvido para validar que o ambiente pertence ao tenant ativo.
///
/// Estratégia de resolução (por prioridade):
/// 1) Header X-Environment-Id (Guid) — fonte preferida para APIs programáticas.
/// 2) Query string ?environmentId=... — fallback para chamadas simples.
/// 3) Sem ambiente → contexto parcial (apenas tenant), aceitável para endpoints globais.
///
/// Validação:
/// - O ambiente deve existir e pertencer ao tenant ativo.
/// - O ambiente deve estar ativo.
/// - Em caso de falha de resolução, o contexto de ambiente não é definido (IsResolved=false).
///   Endpoints operacionais devem exigir contexto completo via policy.
///
/// Segurança:
/// - O header/query-string apenas indica a preferência do cliente.
///   O backend SEMPRE valida que o ambiente pertence ao tenant ativo.
/// - Um cliente malicioso não pode acessar dados de outro tenant passando um EnvironmentId arbitrário.
/// </summary>
public sealed class EnvironmentResolutionMiddleware(
    RequestDelegate next,
    ILogger<EnvironmentResolutionMiddleware> logger)
{
    /// <summary>Nome do header HTTP para transporte do EnvironmentId.</summary>
    public const string EnvironmentIdHeader = "X-Environment-Id";

    /// <summary>Nome do query string parameter para EnvironmentId.</summary>
    public const string EnvironmentIdQueryParam = "environmentId";

    /// <summary>
    /// Processa a requisição resolvendo o ambiente ativo.
    /// EnvironmentContextAccessor e ITenantEnvironmentContextResolver são scoped
    /// e devem ser injetados como parâmetros do InvokeAsync.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        ICurrentTenant currentTenant,
        EnvironmentContextAccessor environmentContextAccessor,
        ITenantEnvironmentContextResolver contextResolver)
    {
        if (TryResolveEnvironmentId(context, out var environmentId))
        {
            await TryResolveAndSetContextAsync(
                context, currentTenant, environmentContextAccessor, contextResolver,
                environmentId);
        }

        await next(context);
    }

    private async Task TryResolveAndSetContextAsync(
        HttpContext context,
        ICurrentTenant currentTenant,
        EnvironmentContextAccessor environmentContextAccessor,
        ITenantEnvironmentContextResolver contextResolver,
        Guid rawEnvironmentId)
    {
        if (currentTenant.Id == Guid.Empty)
        {
            // Tenant não resolvido — não há como validar o ambiente.
            logger.LogDebug(
                "Environment resolution skipped: no active tenant context for EnvironmentId={EnvironmentId}",
                rawEnvironmentId);
            return;
        }

        var tenantId = new TenantId(currentTenant.Id);
        var environmentId = new EnvironmentId(rawEnvironmentId);

        TenantEnvironmentContext? tenantEnvContext;
        try
        {
            tenantEnvContext = await contextResolver.ResolveAsync(
                tenantId, environmentId, context.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            // Request was aborted (client disconnected or timeout). Treat resolution as skipped.
            logger.LogDebug(
                "Environment resolution canceled: request aborted while resolving EnvironmentId={EnvironmentId}",
                rawEnvironmentId);
            return;
        }

        if (tenantEnvContext is null)
        {
            // Ambiente inválido ou não pertence ao tenant — não definir contexto.
            // A auditoria de tentativas suspeitas pode ser adicionada aqui no futuro.
            logger.LogWarning(
                "Environment resolution failed: EnvironmentId={EnvironmentId} does not belong to TenantId={TenantId} or is inactive",
                rawEnvironmentId,
                currentTenant.Id);
            return;
        }

        environmentContextAccessor.Set(
            tenantEnvContext!.EnvironmentId,
            tenantEnvContext!.Profile,
            tenantEnvContext!.IsProductionLike);

        logger.LogDebug(
            "Environment resolved: EnvironmentId={EnvironmentId}, Profile={Profile}, ProductionLike={ProductionLike}",
            rawEnvironmentId,
            tenantEnvContext!.Profile,
            tenantEnvContext!.IsProductionLike);
    }

    private static bool TryResolveEnvironmentId(HttpContext context, out Guid environmentId)
    {
        // Prioridade 1: Header X-Environment-Id
        var headerValue = context.Request.Headers[EnvironmentIdHeader].FirstOrDefault();
        if (Guid.TryParse(headerValue, out environmentId))
            return true;

        // Prioridade 2: Query string ?environmentId=...
        var queryValue = context.Request.Query[EnvironmentIdQueryParam].FirstOrDefault();
        if (Guid.TryParse(queryValue, out environmentId))
            return true;

        environmentId = Guid.Empty;
        return false;
    }
}
