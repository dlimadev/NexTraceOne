using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Contrato de repositório para a entidade AnomalyNarrative.
/// Provê operações de leitura e escrita para narrativas de anomalia geradas por IA.
/// Tipicamente existe uma única narrativa por drift finding.
/// </summary>
public interface IAnomalyNarrativeRepository
{
    /// <summary>Persiste uma nova narrativa de anomalia.</summary>
    Task AddAsync(AnomalyNarrative narrative, CancellationToken cancellationToken = default);

    /// <summary>Obtém a narrativa associada a um drift finding, ou null se não existir.</summary>
    Task<AnomalyNarrative?> GetByDriftFindingIdAsync(DriftFindingId driftFindingId, CancellationToken cancellationToken = default);

    /// <summary>Persiste alterações à narrativa existente.</summary>
    void Update(AnomalyNarrative narrative);
}
