using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>Contrato de repositório para ChangeIntelligenceScore.</summary>
public interface IChangeScoreRepository
{
    /// <summary>Busca o score de uma release.</summary>
    Task<ChangeIntelligenceScore?> GetByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo score.</summary>
    void Add(ChangeIntelligenceScore score);
}
