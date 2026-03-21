using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;

/// <summary>
/// Repositório de provedores externos de IA.
/// </summary>
public interface IExternalAiProviderRepository
{
    /// <summary>Verifica se um provedor existe pelo identificador.</summary>
    Task<bool> ExistsAsync(ExternalAiProviderId id, CancellationToken ct);
}
