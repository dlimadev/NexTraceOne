namespace NexTraceOne.Integrations.Domain;

/// <summary>
/// Contrato para integração com sistemas de canary deployment (Argo Rollouts, Flagger, LaunchDarkly, etc.).
/// A implementação padrão é <c>NullCanaryProvider</c> que retorna lista vazia até que um sistema canary
/// real seja configurado.
/// </summary>
public interface ICanaryProvider
{
    /// <summary>
    /// Retorna os rollouts canary ativos num ambiente específico.
    /// Retorna lista vazia se nenhum sistema canary estiver configurado.
    /// </summary>
    Task<IReadOnlyList<CanaryRolloutInfo>> GetActiveRolloutsAsync(string? environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica se o provider está configurado e ligado a um sistema canary real.
    /// Usado para mostrar nota informativa na UI quando não há integração.
    /// </summary>
    bool IsConfigured { get; }
}

/// <summary>
/// Informação de um rollout canary activo fornecida pelo sistema de canary externo.
/// </summary>
public sealed record CanaryRolloutInfo(
    string Id,
    string ServiceName,
    string Environment,
    string CanaryVersion,
    string StableVersion,
    int CanaryTrafficPct,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt);
