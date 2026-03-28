namespace NexTraceOne.AIKnowledge.Contracts.ExternalAI.ServiceInterfaces;

// IMPLEMENTATION STATUS: Planned — no methods defined, no implementation, no consumers.

/// <summary>
/// Interface pública do módulo ExternalAi.
/// Outros módulos que precisarem de dados deste módulo devem usar
/// este contrato — nunca acessar o DbContext ou repositórios diretamente.
/// </summary>
public interface IExternalAiModule
{
    /// <summary>
    /// Returns all active and inactive external AI providers with status metadata.
    /// </summary>
    Task<IReadOnlyList<ProviderSummaryDto>> GetAvailableProvidersAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns provider health summary for a specific provider.
    /// Returns null when provider does not exist.
    /// </summary>
    Task<ProviderHealthDto?> GetProviderHealthAsync(Guid providerId, CancellationToken ct = default);

    /// <summary>
    /// Selects the best provider for a capability using active policy and provider availability.
    /// Returns null when no eligible provider is found.
    /// </summary>
    Task<RoutingDecisionDto?> RouteRequestAsync(
        string capability,
        string? preferredProvider = null,
        CancellationToken ct = default);
}
