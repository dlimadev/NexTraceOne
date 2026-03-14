using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NexTraceOne.ChangeIntelligence.Domain.Enums;

namespace NexTraceOne.ChangeIntelligence.Application.Abstractions;

/// <summary>Contrato de repositório para janelas de observação pós-release.</summary>
public interface IObservationWindowRepository
{
    /// <summary>Lista janelas de observação de uma release.</summary>
    Task<IReadOnlyList<ObservationWindow>> ListByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default);

    /// <summary>Busca janela por release e fase.</summary>
    Task<ObservationWindow?> GetByReleaseIdAndPhaseAsync(ReleaseId releaseId, ObservationPhase phase, CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma janela de observação.</summary>
    void Add(ObservationWindow window);
}
