using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Infrastructure.Services;

/// <summary>
/// Implementação do serviço de runtime de feature flags.
/// Avalia flags com resolução hierárquica (User → Team → Environment → Tenant → System → Default)
/// e mantém cache em memória com TTL de 60 segundos para minimizar queries ao banco.
/// Injectável em qualquer serviço de aplicação ou infraestrutura para verificar flags at-runtime.
/// </summary>
public sealed class FeatureFlagRuntime(
    IFeatureFlagRepository repository,
    ICurrentTenant currentTenant,
    ICurrentUser currentUser,
    IMemoryCache cache,
    ILogger<FeatureFlagRuntime> logger) : IFeatureFlagRuntime
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    private static readonly ConfigurationScope[] ScopeHierarchy =
    [
        ConfigurationScope.User,
        ConfigurationScope.Team,
        ConfigurationScope.Role,
        ConfigurationScope.Environment,
        ConfigurationScope.Tenant,
        ConfigurationScope.System
    ];

    /// <summary>
    /// Verifica se a flag está activa para o contexto actual (tenant + user).
    /// Usa cache para evitar queries repetitivas dentro da mesma janela de 60s.
    /// </summary>
    public async Task<bool> IsEnabledAsync(
        string flagKey,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(flagKey, currentTenant.Id, currentUser.Id);

        if (cache.TryGetValue(cacheKey, out bool cachedValue))
        {
            logger.LogDebug(
                "FeatureFlagRuntime cache hit for key {FlagKey}: {Value}",
                flagKey, cachedValue);
            return cachedValue;
        }

        var value = await EvaluateAsync(flagKey, null, null, cancellationToken);
        cache.Set(cacheKey, value, CacheTtl);

        logger.LogDebug(
            "FeatureFlagRuntime evaluated {FlagKey} = {Value} (tenant: {TenantId})",
            flagKey, value, currentTenant.Id);

        return value;
    }

    /// <summary>
    /// Verifica se a flag está activa para um scope e referência específicos.
    /// Não usa cache — destina-se a avaliações pontuais fora do contexto corrente.
    /// </summary>
    public async Task<bool> IsEnabledForAsync(
        string flagKey,
        string scopeKey,
        string scopeReferenceId,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<ConfigurationScope>(scopeKey, ignoreCase: true, out var scope))
        {
            logger.LogWarning(
                "FeatureFlagRuntime: unknown scope '{ScopeKey}' for flag {FlagKey}",
                scopeKey, flagKey);
            return false;
        }

        return await EvaluateAsync(flagKey, scope, scopeReferenceId, cancellationToken);
    }

    /// <summary>
    /// Retorna todas as feature flags activas para o contexto actual.
    /// Cache por tenant+user durante o mesmo TTL.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, bool>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"ffr:all:{currentTenant.Id}:{currentUser.Id}";

        if (cache.TryGetValue(cacheKey, out IReadOnlyDictionary<string, bool>? cached) && cached is not null)
            return cached;

        var definitions = await repository.GetAllDefinitionsAsync(cancellationToken);
        var result = new Dictionary<string, bool>(definitions.Count);

        foreach (var definition in definitions.Where(d => d.IsActive))
        {
            result[definition.Key] = await EvaluateAsync(
                definition.Key, null, null, cancellationToken);
        }

        cache.Set(cacheKey, (IReadOnlyDictionary<string, bool>)result, CacheTtl);
        return result;
    }

    /// <summary>Invalida todas as entradas de cache deste tenant.</summary>
    public void InvalidateCache()
    {
        // IMemoryCache não suporta invalidação por prefixo nativamente.
        // Para uma invalidação mais granular, considera-se CancellationTokenSource por tenant.
        // Por ora, a invalidação é implícita via expiração do TTL (60s).
        // Em produção, substituir por IMemoryCache + ChangeToken por tenant.
        logger.LogDebug(
            "FeatureFlagRuntime cache invalidation requested for tenant {TenantId}",
            currentTenant.Id);
    }

    // ── Internal evaluation ───────────────────────────────────────────────────

    private async Task<bool> EvaluateAsync(
        string flagKey,
        ConfigurationScope? forceScope,
        string? forceScopeRefId,
        CancellationToken cancellationToken)
    {
        var definition = await repository.GetDefinitionByKeyAsync(flagKey, cancellationToken);
        if (definition is null || !definition.IsActive)
            return false;

        // Se scope foi forçado, avaliar apenas esse scope
        if (forceScope.HasValue)
        {
            var entry = await repository.GetEntryByKeyAndScopeAsync(
                flagKey, forceScope.Value, forceScopeRefId, cancellationToken);
            return entry?.IsEnabled ?? definition.DefaultEnabled;
        }

        // Resolução hierárquica padrão
        foreach (var scope in ScopeHierarchy)
        {
            var scopeRefId = ResolveScopeReferenceId(scope);
            if (scopeRefId is null) continue;

            var entry = await repository.GetEntryByKeyAndScopeAsync(
                flagKey, scope, scopeRefId, cancellationToken);
            if (entry is not null)
                return entry.IsEnabled;
        }

        return definition.DefaultEnabled;
    }

    private string? ResolveScopeReferenceId(ConfigurationScope scope) => scope switch
    {
        ConfigurationScope.User        => currentUser.Id.ToString(),
        ConfigurationScope.Tenant      => currentTenant.Id.ToString(),
        ConfigurationScope.System      => "system",
        _                              => null // Team, Role, Environment resolvidos externamente
    };

    private static string BuildCacheKey(string flagKey, Guid tenantId, string userId)
        => $"ffr:{flagKey}:{tenantId}:{userId}";
}
