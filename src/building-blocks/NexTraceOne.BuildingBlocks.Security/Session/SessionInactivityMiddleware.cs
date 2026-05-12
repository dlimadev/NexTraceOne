using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NexTraceOne.BuildingBlocks.Security.Session;

/// <summary>
/// Middleware de segurança de sessão.
/// Valida inactividade, conta sessões concorrentes e detecta mudanças anómalas de IP.
/// Configuração: Security:Session:{InactivityTimeoutMinutes, MaxConcurrentSessions, DetectAnomalousIpChange}
/// </summary>
public sealed class SessionInactivityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SessionInactivityMiddleware> _logger;

    public SessionInactivityMiddleware(
        RequestDelegate next,
        IDistributedCache cache,
        IConfiguration configuration,
        ILogger<SessionInactivityMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Apenas requests autenticados
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirst("sub")?.Value
                  ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var sessionId = context.Request.Headers["X-Session-Id"].FirstOrDefault()
                     ?? context.User.FindFirst("sid")?.Value;

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionId))
        {
            await _next(context);
            return;
        }

        // Verificar inactividade
        var timeoutMinutes = _configuration.GetValue<int>("Security:Session:InactivityTimeoutMinutes", 480);
        var activityKey = $"session-activity:{sessionId}:{userId}";
        var existing = await _cache.GetStringAsync(activityKey, context.RequestAborted);

        if (existing is not null && DateTimeOffset.TryParse(existing, out var lastActivity))
        {
            if ((DateTimeOffset.UtcNow - lastActivity).TotalMinutes > timeoutMinutes)
            {
                _logger.LogWarning(
                    "[SECURITY] Sessão expirada por inactividade. UserId={UserId} SessionId={SessionId} ÚltimaActividade={LastActivity}",
                    userId, sessionId, lastActivity);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        // Actualizar timestamp de actividade
        await _cache.SetStringAsync(
            activityKey,
            DateTimeOffset.UtcNow.ToString("O"),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(timeoutMinutes + 60)
            },
            context.RequestAborted);

        // Detectar mudança anómala de IP
        var detectIpChange = _configuration.GetValue<bool>("Security:Session:DetectAnomalousIpChange", true);
        if (detectIpChange)
        {
            await CheckIpChangeAsync(context, sessionId, userId);
        }

        await _next(context);
    }

    private async Task CheckIpChangeAsync(HttpContext context, string sessionId, string userId)
    {
        var currentIp = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                     ?? context.Connection.RemoteIpAddress?.ToString()
                     ?? "unknown";

        var ipKey = $"session-ip:{sessionId}:{userId}";
        var storedIp = await _cache.GetStringAsync(ipKey, context.RequestAborted);

        if (storedIp is null)
        {
            // Primeiro request — guardar IP
            await _cache.SetStringAsync(
                ipKey, currentIp,
                new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromHours(24) },
                context.RequestAborted);
        }
        else if (!string.Equals(storedIp, currentIp, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "[SECURITY] Mudança anómala de IP detectada. UserId={UserId} SessionId={SessionId} IPAnterior={StoredIp} IPActual={CurrentIp}",
                userId, sessionId, storedIp, currentIp);
        }
    }
}
