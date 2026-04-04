using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>Contrato de repositório para registos de canary rollout de releases.</summary>
public interface ICanaryRolloutRepository
{
    /// <summary>Obtém o registo de canary rollout mais recente de uma release.</summary>
    Task<CanaryRollout?> GetLatestByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default);

    /// <summary>Lista todos os registos de canary rollout de uma release (histórico).</summary>
    Task<IReadOnlyList<CanaryRollout>> ListByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo registo de canary rollout.</summary>
    void Add(CanaryRollout rollout);
}
