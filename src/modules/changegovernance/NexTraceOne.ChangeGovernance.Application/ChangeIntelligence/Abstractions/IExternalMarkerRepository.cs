using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>Contrato de repositório para marcadores externos de ferramentas CI/CD.</summary>
public interface IExternalMarkerRepository
{
    /// <summary>Busca marcadores de uma release.</summary>
    Task<IReadOnlyList<ExternalMarker>> ListByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um marcador externo.</summary>
    void Add(ExternalMarker marker);
}
