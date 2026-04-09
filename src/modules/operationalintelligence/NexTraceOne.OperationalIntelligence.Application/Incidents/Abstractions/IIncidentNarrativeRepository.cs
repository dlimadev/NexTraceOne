using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

/// <summary>
/// Repositório de narrativas de incidentes geradas por IA — contrato do domínio
/// para persistência e consulta de IncidentNarrative.
/// Tipicamente existe uma única narrativa por incidente.
/// </summary>
public interface IIncidentNarrativeRepository
{
    /// <summary>Persiste uma nova narrativa de incidente.</summary>
    Task AddAsync(IncidentNarrative narrative, CancellationToken cancellationToken = default);

    /// <summary>Obtém uma narrativa pelo seu identificador.</summary>
    Task<IncidentNarrative?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Obtém a narrativa associada a um incidente, ou null se não existir.</summary>
    Task<IncidentNarrative?> GetByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken = default);

    /// <summary>Persiste alterações à narrativa existente.</summary>
    Task UpdateAsync(IncidentNarrative narrative, CancellationToken cancellationToken = default);
}
