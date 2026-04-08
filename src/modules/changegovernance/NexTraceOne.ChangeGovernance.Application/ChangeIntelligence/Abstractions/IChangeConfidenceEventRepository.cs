using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>
/// Repositório para eventos de confiança de mudanças (append-only).
/// </summary>
public interface IChangeConfidenceEventRepository
{
    /// <summary>Lista todos os eventos de confiança de uma release, ordenados cronologicamente.</summary>
    Task<IReadOnlyList<ChangeConfidenceEvent>> ListByReleaseAsync(ReleaseId releaseId, CancellationToken cancellationToken);

    /// <summary>Obtém o evento de confiança mais recente de uma release.</summary>
    Task<ChangeConfidenceEvent?> GetLatestByReleaseAsync(ReleaseId releaseId, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo evento de confiança (append-only).</summary>
    Task AddAsync(ChangeConfidenceEvent confidenceEvent, CancellationToken cancellationToken);
}
