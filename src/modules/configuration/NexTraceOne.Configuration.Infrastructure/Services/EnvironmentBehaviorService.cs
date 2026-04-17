using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Infrastructure.Services;

/// <summary>
/// Implementação do serviço de parâmetros de comportamento por ambiente.
/// Delega a resolução hierárquica ao <see cref="IConfigurationResolutionService"/>
/// e fornece semântica tipada para leitura de chaves <c>env.behavior.*</c>.
/// </summary>
internal sealed class EnvironmentBehaviorService(
    IConfigurationResolutionService resolutionService)
    : IEnvironmentBehaviorService
{
    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(
        string key,
        string? environmentId,
        CancellationToken cancellationToken)
    {
        var scope = environmentId is not null
            ? ConfigurationScope.Environment
            : ConfigurationScope.System;

        var result = await resolutionService.ResolveEffectiveValueAsync(
            key, scope, environmentId, cancellationToken);

        // Chave não definida ou sem valor → habilitado por padrão (fail-open)
        if (result?.EffectiveValue is null)
            return true;

        return result.EffectiveValue.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<int> GetIntAsync(
        string key,
        string? environmentId,
        int defaultValue,
        CancellationToken cancellationToken)
    {
        var scope = environmentId is not null
            ? ConfigurationScope.Environment
            : ConfigurationScope.System;

        var result = await resolutionService.ResolveEffectiveValueAsync(
            key, scope, environmentId, cancellationToken);

        if (result?.EffectiveValue is null)
            return defaultValue;

        return int.TryParse(result.EffectiveValue, out var parsed) ? parsed : defaultValue;
    }
}
