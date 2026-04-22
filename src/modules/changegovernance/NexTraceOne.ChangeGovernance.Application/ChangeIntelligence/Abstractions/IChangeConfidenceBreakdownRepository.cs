using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>Contrato de repositório para ChangeConfidenceBreakdown (Score 2.0).</summary>
public interface IChangeConfidenceBreakdownRepository
{
    /// <summary>Busca o breakdown de confiança para uma release específica, incluindo sub-scores.</summary>
    Task<ChangeConfidenceBreakdown?> GetByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default);

    /// <summary>Busca breakdowns de confiança para um conjunto de releases (batch lookup para relatórios).</summary>
    Task<IReadOnlyList<ChangeConfidenceBreakdown>> ListByReleaseIdsAsync(
        IEnumerable<ReleaseId> releaseIds, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo breakdown de confiança.</summary>
    void Add(ChangeConfidenceBreakdown breakdown);
}
