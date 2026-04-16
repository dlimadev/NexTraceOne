namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>
/// Serviço de runtime para avaliação de feature flags.
/// Permite que qualquer parte da aplicação verifique se uma feature está activa
/// para o contexto corrente (tenant, ambiente, utilizador, equipa).
/// Implementa cache in-memory com TTL para evitar round-trips repetitivos ao banco.
/// </summary>
public interface IFeatureFlagRuntime
{
    /// <summary>
    /// Verifica se uma feature flag está activa para o contexto actual.
    /// Aplica resolução hierárquica: User → Team → Role → Environment → Tenant → System → Default.
    /// </summary>
    /// <param name="flagKey">Chave única da feature flag (ex: "ai.assistant.enabled").</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>true se a flag está activa para o contexto actual; false caso contrário.</returns>
    Task<bool> IsEnabledAsync(string flagKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se uma feature flag está activa para um scope e referência específicos.
    /// Útil quando o contexto de avaliação difere do contexto da requisição actual.
    /// </summary>
    /// <param name="flagKey">Chave única da feature flag.</param>
    /// <param name="scopeKey">Scope de avaliação (ex: "tenant", "environment", "user").</param>
    /// <param name="scopeReferenceId">Identificador do scope (ex: tenant ID, user ID).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<bool> IsEnabledForAsync(
        string flagKey,
        string scopeKey,
        string scopeReferenceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna todas as feature flags activas para o contexto actual.
    /// Útil para bootstrap do frontend ou auditorias.
    /// </summary>
    Task<IReadOnlyDictionary<string, bool>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalida a cache de feature flags forçando releitura do banco na próxima avaliação.
    /// Deve ser chamado após alterações via SetFeatureFlagOverride.
    /// </summary>
    void InvalidateCache();
}
