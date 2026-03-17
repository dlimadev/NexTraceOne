namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Serviço de monitoramento de saúde dos providers de IA.
/// </summary>
public interface IAiProviderHealthService
{
    /// <summary>Verifica a saúde de todos os providers registrados.</summary>
    Task<IReadOnlyList<AiProviderHealthResult>> CheckAllProvidersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Verifica a saúde de um provider específico.</summary>
    Task<AiProviderHealthResult> CheckProviderAsync(
        string providerId,
        CancellationToken cancellationToken = default);
}
