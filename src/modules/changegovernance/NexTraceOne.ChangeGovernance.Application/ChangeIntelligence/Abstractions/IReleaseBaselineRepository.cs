using NexTraceOne.ChangeIntelligence.Domain.Entities;

namespace NexTraceOne.ChangeIntelligence.Application.Abstractions;

/// <summary>Contrato de repositório para baselines de release.</summary>
public interface IReleaseBaselineRepository
{
    /// <summary>Busca o baseline de uma release.</summary>
    Task<ReleaseBaseline?> GetByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um baseline.</summary>
    void Add(ReleaseBaseline baseline);
}
