using NexTraceOne.Integrations.Domain;

namespace NexTraceOne.Integrations.Infrastructure;

/// <summary>
/// Implementação nula de ICanaryProvider.
/// Retorna lista vazia enquanto nenhum sistema canary real (Argo Rollouts, Flagger, LaunchDarkly, etc.)
/// estiver configurado. Registado como default via DI.
/// Substitua por uma implementação concreta quando o sistema canary estiver disponível.
/// </summary>
internal sealed class NullCanaryProvider : ICanaryProvider
{
    /// <inheritdoc />
    public bool IsConfigured => false;

    /// <inheritdoc />
    public Task<IReadOnlyList<CanaryRolloutInfo>> GetActiveRolloutsAsync(
        string? environment,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CanaryRolloutInfo>>([]);
}
