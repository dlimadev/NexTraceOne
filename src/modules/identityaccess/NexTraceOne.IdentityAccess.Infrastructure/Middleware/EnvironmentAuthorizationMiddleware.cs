using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using System.Security.Claims;

namespace NexTraceOne.IdentityAccess.Infrastructure.Middleware;

/// <summary>
/// Middleware que aplica políticas de acesso por ambiente (W5-05).
/// 
/// Para cada requisição com header X-Environment:
/// 1. Lê a política aplicável para o tenant + ambiente
/// 2. Verifica se as roles do utilizador estão permitidas
/// 3. Se role requer JIT e não há sessão JIT activa → cria JitAccessRequest automático
/// 4. Retorna 403 Forbidden se política violada
/// 
/// Deve ser registado APÓS EnvironmentResolutionMiddleware e APÓS UseAuthentication.
/// </summary>
public sealed class EnvironmentAuthorizationMiddleware(
    RequestDelegate next,
    ILogger<EnvironmentAuthorizationMiddleware> logger)
{
    /// <summary>Header HTTP que indica o ambiente alvo da operação.</summary>
    public const string EnvironmentHeader = "X-Environment";

    /// <summary>
    /// Processa a requisição validando políticas de acesso por ambiente.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        ICurrentTenant currentTenant,
        IEnvironmentAccessPolicyRepository policyRepository,
        IJitAccessRequestRepository jitRepository)
    {
        // Só valida se houver tenant resolvido e utilizador autenticado
        if (currentTenant.Id == Guid.Empty || !context.User.Identity?.IsAuthenticated == true)
        {
            await next(context);
            return;
        }

        // Tenta ler o ambiente do header
        var environmentName = context.Request.Headers[EnvironmentHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            // Sem header de ambiente → passa sem validação adicional
            await next(context);
            return;
        }

        // Obtém roles do utilizador dos claims
        var userRoles = context.User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        if (userRoles.Count == 0)
        {
            // Sem roles definidas → permite passar (pode ser endpoint público)
            logger.LogDebug(
                "Environment authorization skipped: no roles found for user {UserId}",
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            await next(context);
            return;
        }

        // Carrega políticas activas do tenant
        var policies = await policyRepository.ListByTenantAsync(
            currentTenant.Id, context.RequestAborted);

        // Encontra política aplicável para este ambiente
        var applicablePolicy = policies.FirstOrDefault(p =>
            p.Environments.Contains(environmentName, StringComparer.OrdinalIgnoreCase));

        if (applicablePolicy is null)
        {
            // Nenhuma política específica para este ambiente → permite acesso
            logger.LogDebug(
                "No environment policy found for tenant {TenantId} and environment {Environment}",
                currentTenant.Id, environmentName);
            await next(context);
            return;
        }

        // Verifica se alguma role do utilizador está na lista de roles permitidas
        var hasAllowedRole = userRoles.Any(role =>
            applicablePolicy.AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase));

        if (!hasAllowedRole)
        {
            logger.LogWarning(
                "Environment access denied: user roles [{UserRoles}] not allowed for environment {Environment} by policy {PolicyName}",
                string.Join(", ", userRoles),
                environmentName,
                applicablePolicy.PolicyName);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync(
                $"Access to environment '{environmentName}' is not permitted with your current roles.");
            return;
        }

        // Verifica se alguma role requer JIT approval
        var requiresJit = userRoles.Any(role =>
            applicablePolicy.RequireJitForRoles.Contains(role, StringComparer.OrdinalIgnoreCase));

        if (requiresJit)
        {
            // Verifica se já existe sessão JIT activa para este utilizador + permissão
            var userId = new UserId(Guid.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? throw new InvalidOperationException("User ID claim missing")));
            
            var hasActiveJit = await HasActiveJitSessionAsync(
                jitRepository, userId, currentTenant.Id, environmentName, userRoles);

            if (!hasActiveJit)
            {
                // Cria request JIT automático
                logger.LogInformation(
                    "Auto-creating JIT request for user {UserId} accessing environment {Environment} with roles requiring approval",
                    userId.Value, environmentName);

                await CreateAutomaticJitRequestAsync(
                    jitRepository, userId, currentTenant.Id, environmentName, 
                    userRoles, applicablePolicy, context.RequestAborted);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync(
                    $"Access to environment '{environmentName}' requires JIT approval. " +
                    $"An automatic request has been created. Please check your pending approvals.");
                return;
            }
        }

        // Acesso autorizado
        await next(context);
    }

    private static async Task<bool> HasActiveJitSessionAsync(
        IJitAccessRequestRepository jitRepository,
        UserId userId,
        Guid tenantId,
        string environmentName,
        List<string> userRoles)
    {
        // Verifica se existe algum request JIT aprovado e activo para este utilizador
        var activeSessions = await jitRepository.ListActiveByUserAsync(
            userId, new TenantId(tenantId), CancellationToken.None);
        
        // Se houver pelo menos uma sessão activa, permite acesso
        return activeSessions.Count > 0;
    }

    private static async Task CreateAutomaticJitRequestAsync(
        IJitAccessRequestRepository jitRepository,
        UserId userId,
        Guid tenantId,
        string environmentName,
        List<string> userRoles,
        EnvironmentAccessPolicy policy,
        CancellationToken cancellationToken)
    {
        // Cria um request JIT para cada role que requer aprovação
        foreach (var role in userRoles.Where(r => 
            policy.RequireJitForRoles.Contains(r, StringComparer.OrdinalIgnoreCase)))
        {
            var jitRequest = JitAccessRequest.Create(
                requestedBy: userId,
                tenantId: new TenantId(tenantId),
                permissionCode: $"environment:{environmentName}:{role}",
                scope: $"Access to environment '{environmentName}' with role '{role}'",
                justification: $"Automatic JIT request triggered by environment access policy '{policy.PolicyName}'",
                now: DateTimeOffset.UtcNow);

            await jitRepository.AddAsync(jitRequest, cancellationToken);
        }
    }
}
