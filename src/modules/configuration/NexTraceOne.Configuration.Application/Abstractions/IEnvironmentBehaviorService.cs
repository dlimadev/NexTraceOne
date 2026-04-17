namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>
/// Serviço de abstração para leitura de parâmetros de comportamento por ambiente.
/// Encapsula a resolução hierárquica de chaves <c>env.behavior.*</c> via
/// <see cref="IConfigurationResolutionService"/> com semântica tipada.
///
/// Regra arquitetural: parâmetros de IA NÃO são scoped por ambiente.
/// Este serviço trata apenas de comportamentos operacionais do ambiente.
/// </summary>
public interface IEnvironmentBehaviorService
{
    /// <summary>
    /// Verifica se um comportamento está habilitado para o ambiente especificado.
    /// Resolve hierarquicamente: Environment → System.
    /// Se <paramref name="environmentId"/> for <c>null</c>, resolve apenas em System scope.
    /// Quando a chave não existe ou não tem valor, retorna <c>true</c> (habilitado por padrão).
    /// </summary>
    /// <param name="key">Chave de configuração, e.g. <c>env.behavior.change.ingest.enabled</c>.</param>
    /// <param name="environmentId">Identificador do ambiente (Guid como string). Pode ser null.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<bool> IsEnabledAsync(string key, string? environmentId, CancellationToken cancellationToken);

    /// <summary>
    /// Obtém um valor inteiro de comportamento para o ambiente especificado.
    /// Resolve hierarquicamente: Environment → System.
    /// Se <paramref name="environmentId"/> for <c>null</c>, resolve apenas em System scope.
    /// Retorna <paramref name="defaultValue"/> se a chave não existir ou o valor não for um inteiro válido.
    /// </summary>
    /// <param name="key">Chave de configuração, e.g. <c>env.behavior.data.telemetry_retention_days</c>.</param>
    /// <param name="environmentId">Identificador do ambiente (Guid como string). Pode ser null.</param>
    /// <param name="defaultValue">Valor padrão quando a chave não for encontrada ou inválida.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<int> GetIntAsync(string key, string? environmentId, int defaultValue, CancellationToken cancellationToken);
}
