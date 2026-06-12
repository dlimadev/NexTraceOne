namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Monitor singleton de saúde dos providers de IA.
/// Mantém cache atualizado pelo background service de health checks.
/// </summary>
public interface IAiProviderHealthMonitor
{
    /// <summary>Retorna o estado de saúde de todos os providers conhecidos.</summary>
    IReadOnlyList<AiProviderHealthResult> GetAllHealthStatuses();

    /// <summary>Retorna o estado de saúde de um provider específico.</summary>
    AiProviderHealthResult? GetHealthStatus(string providerId);

    /// <summary>Verifica se um provider está saudável no momento.</summary>
    bool IsHealthy(string providerId);

    /// <summary>Data/hora da última verificação de saúde.</summary>
    DateTimeOffset? LastCheckTime { get; }

    /// <summary>Lista IDs dos providers que estão saudáveis no momento.</summary>
    IReadOnlyList<string> GetHealthyProviderIds();
}
