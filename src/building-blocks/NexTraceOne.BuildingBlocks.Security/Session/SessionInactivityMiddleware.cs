using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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

        // Verificar limite de sessões concorrentes
        var maxSessions = _configuration.GetValue<int>("Security:Session:MaxConcurrentSessions", 5);
        await EnforceMaxConcurrentSessionsAsync(userId, sessionId, maxSessions, context.RequestAborted);

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
                
                // Revogar sessão no cache
                await RevokeSessionAsync(userId, sessionId, context.RequestAborted);
                
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

    /// <summary>
    /// Garante que o utilizador não excede o número máximo de sessões simultâneas.
    /// Se exceder, revoga a sessão mais antiga.
    /// </summary>
    private async Task EnforceMaxConcurrentSessionsAsync(string userId, string currentSessionId, int maxSessions, CancellationToken cancellationToken)
    {
        var sessionsKey = $"user-sessions:{userId}";
        var sessionsJson = await _cache.GetStringAsync(sessionsKey, cancellationToken);
        
        var activeSessions = sessionsJson is not null
            ? JsonSerializer.Deserialize<List<string>>(sessionsJson) ?? new List<string>()
            : new List<string>();

        // Remover sessões expiradas do registo
        var validSessions = new List<string>();
        foreach (var sid in activeSessions)
        {
            var activityKey = $"session-activity:{sid}:{userId}";
            var activity = await _cache.GetStringAsync(activityKey, cancellationToken);
            if (activity is not null)
            {
                validSessions.Add(sid);
            }
        }

        // Adicionar sessão actual se não estiver na lista
        if (!validSessions.Contains(currentSessionId))
        {
            validSessions.Add(currentSessionId);
        }

        // Se exceder o limite, remover as mais antigas (FIFO)
        while (validSessions.Count > maxSessions)
        {
            var oldestSession = validSessions[0];
            _logger.LogInformation(
                "[SECURITY] Limite de sessões excedido. A revogar sessão mais antiga. UserId={UserId} SessãoRevogada={OldestSession} Limite={MaxSessions}",
                userId, oldestSession, maxSessions);
            
            await RevokeSessionAsync(userId, oldestSession, cancellationToken);
            validSessions.RemoveAt(0);
        }

        // Guardar lista actualizada
        await _cache.SetStringAsync(
            sessionsKey,
            JsonSerializer.Serialize(validSessions),
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromHours(24) },
            cancellationToken);
    }

    /// <summary>
    /// Revoga uma sessão removendo os seus dados do cache.
    /// </summary>
    private async Task RevokeSessionAsync(string userId, string sessionId, CancellationToken cancellationToken)
    {
        var activityKey = $"session-activity:{sessionId}:{userId}";
        var ipKey = $"session-ip:{sessionId}:{userId}";
        
        await _cache.RemoveAsync(activityKey, cancellationToken);
        await _cache.RemoveAsync(ipKey, cancellationToken);
        
        _logger.LogInformation(
            "[SECURITY] Sessão revogada. UserId={UserId} SessionId={SessionId}",
            userId, sessionId);
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
            
            // Emitir evento de segurança via reflection para evitar dependência circular
            try
            {
                var securityEventRepoType = Type.GetType("NexTraceOne.IdentityAccess.Application.Abstractions.ISecurityEventRepository, NexTraceOne.IdentityAccess.Application");
                if (securityEventRepoType is not null)
                {
                    var scopeFactory = context.RequestServices.GetService(typeof(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory)) 
                        as Microsoft.Extensions.DependencyInjection.IServiceScopeFactory;
                    
                    if (scopeFactory is not null)
                    {
                        using var scope = scopeFactory.CreateScope();
                        var repo = scope.ServiceProvider.GetService(securityEventRepoType);
                        
                        if (repo is not null)
                        {
                            var createMethod = securityEventRepoType.GetMethod("CreateAsync");
                            if (createMethod is not null)
                            {
                                var eventType = Type.GetType("NexTraceOne.IdentityAccess.Domain.Enums.SecurityEventType, NexTraceOne.IdentityAccess.Domain");
                                var eventEnumValue = Enum.Parse(eventType!, "AnomalousIpChange");
                                
                                var metadata = new Dictionary<string, object>
                                {
                                    ["sessionId"] = sessionId,
                                    ["previousIp"] = storedIp,
                                    ["newIp"] = currentIp,
                                    ["timestamp"] = DateTimeOffset.UtcNow
                                };
                                
                                var task = createMethod.Invoke(repo, new[] {
                                    eventEnumValue,
                                    userId,
                                    null, // tenantId opcional
                                    JsonSerializer.Serialize(metadata),
                                    context.RequestAborted
                                }) as Task;
                                
                                if (task is not null)
                                {
                                    await task;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SECURITY] Erro ao emitir evento de segurança para mudança de IP");
            }
        }
    }
}
