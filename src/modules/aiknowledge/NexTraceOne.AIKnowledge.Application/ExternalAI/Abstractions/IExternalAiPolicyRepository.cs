using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;

/// <summary>
/// Repositório de políticas de governança de IA externa.
/// </summary>
public interface IExternalAiPolicyRepository
{
    /// <summary>Obtém uma política pelo nome (case-sensitive).</summary>
    Task<ExternalAiPolicy?> GetByNameAsync(string name, CancellationToken ct);

    /// <summary>Adiciona e persiste uma nova política.</summary>
    Task AddAsync(ExternalAiPolicy policy, CancellationToken ct);

    /// <summary>Actualiza e persiste uma política existente.</summary>
    Task UpdateAsync(ExternalAiPolicy policy, CancellationToken ct);
}
