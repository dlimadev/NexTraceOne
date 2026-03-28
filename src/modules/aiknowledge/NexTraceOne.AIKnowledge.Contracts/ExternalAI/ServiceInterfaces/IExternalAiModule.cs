namespace NexTraceOne.AIKnowledge.Contracts.ExternalAI.ServiceInterfaces;

// IMPLEMENTATION STATUS: Complete — implemented by ExternalAiModule, registered in DI, covered by ExternalAiModuleTests.

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
    /// When <paramref name="environment"/> is "production", stricter approval rules are applied —
    /// any active policy matching the capability that requires approval will block routing.
    /// Returns null when no eligible provider is found or when policy blocks the request.
    /// </summary>
    Task<RoutingDecisionDto?> RouteRequestAsync(
        string capability,
        string? preferredProvider = null,
        string? environment = null,
        CancellationToken ct = default);
}
