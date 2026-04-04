using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>Contrato de repositório para estados de feature flags de releases.</summary>
public interface IFeatureFlagStateRepository
{
    /// <summary>Obtém o estado de feature flags mais recente de uma release.</summary>
    Task<ReleaseFeatureFlagState?> GetLatestByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo estado de feature flags.</summary>
    void Add(ReleaseFeatureFlagState state);
}
