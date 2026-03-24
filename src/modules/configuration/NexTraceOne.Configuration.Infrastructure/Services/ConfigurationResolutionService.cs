using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Infrastructure.Services;

/// <summary>
/// Implementação do serviço de resolução hierárquica de configurações.
/// Percorre a hierarquia de âmbitos (User → Team → Role → Environment → Tenant → System)
/// para encontrar o valor efetivo, respeitando herança e valores padrão.
/// </summary>
internal sealed class ConfigurationResolutionService(
    IConfigurationDefinitionRepository definitionRepository,
    IConfigurationEntryRepository entryRepository,
    IConfigurationCacheService cacheService)
    : IConfigurationResolutionService
{
    /// <summary>
    /// Hierarquia de âmbitos ordenada do mais específico para o mais genérico.
    /// A resolução percorre esta lista até encontrar um valor ativo.
    /// </summary>
    private static readonly ConfigurationScope[] ScopeHierarchy =
    [
        ConfigurationScope.User,
        ConfigurationScope.Team,
        ConfigurationScope.Role,
        ConfigurationScope.Environment,
        ConfigurationScope.Tenant,
        ConfigurationScope.System
    ];

    public async Task<EffectiveConfigurationDto?> ResolveEffectiveValueAsync(
        string key,
        ConfigurationScope scope,
        string? scopeReferenceId,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"cfg:resolve:{key}:{scope}:{scopeReferenceId ?? "null"}";

        return await cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var definition = await definitionRepository.GetByKeyAsync(key, cancellationToken);
            if (definition is null)
            {
                return null;
            }

            var entries = await entryRepository.GetAllByKeyAsync(key, cancellationToken);
            var now = DateTimeOffset.UtcNow;

            var startIndex = Array.IndexOf(ScopeHierarchy, scope);
            if (startIndex < 0)
            {
                startIndex = 0;
            }

            for (var i = startIndex; i < ScopeHierarchy.Length; i++)
            {
                var currentScope = ScopeHierarchy[i];

                if (!definition.AllowedScopes.Contains(currentScope))
                {
                    continue;
                }

                var scopeRef = currentScope == scope ? scopeReferenceId : null;

                var entry = entries.FirstOrDefault(e =>
                    e.Scope == currentScope
                    && e.ScopeReferenceId == scopeRef
                    && e.IsActive
                    && (e.EffectiveFrom is null || e.EffectiveFrom <= now)
                    && (e.EffectiveTo is null || e.EffectiveTo > now));

                if (entry is not null)
                {
                    return new EffectiveConfigurationDto(
                        Key: key,
                        EffectiveValue: entry.Value,
                        ResolvedScope: currentScope.ToString(),
                        ResolvedScopeReferenceId: scopeRef is not null ? Guid.TryParse(scopeRef, out var parsed) ? parsed : null : null,
                        IsInherited: currentScope != scope,
                        IsDefault: false,
                        DefinitionKey: definition.Key,
                        ValueType: definition.ValueType.ToString(),
                        IsSensitive: definition.IsSensitive,
                        Version: entry.Version);
                }
            }

            if (definition.DefaultValue is not null)
            {
                return new EffectiveConfigurationDto(
                    Key: key,
                    EffectiveValue: definition.DefaultValue,
                    ResolvedScope: ConfigurationScope.System.ToString(),
                    ResolvedScopeReferenceId: null,
                    IsInherited: scope != ConfigurationScope.System,
                    IsDefault: true,
                    DefinitionKey: definition.Key,
                    ValueType: definition.ValueType.ToString(),
                    IsSensitive: definition.IsSensitive,
                    Version: 0);
            }

            return null;
        }, cancellationToken);
    }

    public async Task<List<EffectiveConfigurationDto>> ResolveAllEffectiveAsync(
        ConfigurationScope scope,
        string? scopeReferenceId,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"cfg:resolve-all:{scope}:{scopeReferenceId ?? "null"}";

        return await cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var definitions = await definitionRepository.GetAllAsync(cancellationToken);
            var results = new List<EffectiveConfigurationDto>(definitions.Count);

            foreach (var definition in definitions)
            {
                var resolved = await ResolveEffectiveValueAsync(
                    definition.Key, scope, scopeReferenceId, cancellationToken);

                if (resolved is not null)
                {
                    results.Add(resolved);
                }
            }

            return results;
        }, cancellationToken);
    }
}
