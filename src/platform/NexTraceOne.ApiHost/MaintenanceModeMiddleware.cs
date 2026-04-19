using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.ApiHost;

/// <summary>
/// Middleware que verifica se a plataforma está em modo de manutenção.
/// Lê a configuração <c>platform.maintenance_mode</c> (scope=System) via
/// <see cref="IConfigurationResolutionService"/>. Em caso de falha na leitura,
/// falha-aberto: a requisição prossegue normalmente (fail-open).
/// Rotas <c>/health</c>, <c>/ready</c> e <c>/preflight</c> são sempre permitidas.
/// Retorna HTTP 503 com corpo JSON quando o modo de manutenção está activo.
/// Deve ser registado após <c>EnvironmentResolutionMiddleware</c> e antes de <c>UseAuthorization</c>.
/// </summary>
public sealed class MaintenanceModeMiddleware(RequestDelegate next, ILogger<MaintenanceModeMiddleware> logger)
{
    private static readonly string[] BypassPaths =
    [
        "/health",
        "/ready",
        "/preflight",
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsExemptPath(context.Request.Path))
        {
            await next(context);
            return;
        }

        var enabled = await IsMaintenanceModeEnabledAsync(context);

        if (!enabled)
        {
            await next(context);
            return;
        }

        logger.LogInformation("Maintenance mode active — request to {Path} rejected with 503.", context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(
            """{"code":"Platform.MaintenanceMode","message":"The platform is currently in maintenance mode. Please try again later."}""",
            context.RequestAborted);
    }

    private static bool IsExemptPath(PathString path)
    {
        foreach (var bypassPath in BypassPaths)
        {
            if (path.StartsWithSegments(bypassPath, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private async Task<bool> IsMaintenanceModeEnabledAsync(HttpContext context)
    {
        try
        {
            var resolutionService = context.RequestServices.GetService<IConfigurationResolutionService>();
            if (resolutionService is null)
                return false; // fail-open

            var dto = await resolutionService.ResolveEffectiveValueAsync(
                "platform.maintenance_mode",
                ConfigurationScope.System,
                scopeReferenceId: null,
                CancellationToken.None);

            if (dto is null || string.IsNullOrWhiteSpace(dto.EffectiveValue))
                return false; // fail-open

            return string.Equals(dto.EffectiveValue, "true", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            // Fail-open: log para diagnóstico mas não bloqueia a requisição
            logger.LogWarning(ex, "Failed to read platform.maintenance_mode configuration — failing open.");
            return false;
        }
    }
}
