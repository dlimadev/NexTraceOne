using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

/// <summary>
/// Repositório para persistência e leitura de MitigationWorkflowRecord.
/// Separa a responsabilidade de workflows de mitigação do IIncidentStore genérico.
/// </summary>
public interface IMitigationWorkflowRepository
{
    /// <summary>Persiste um novo workflow de mitigação.</summary>
    Task AddAsync(MitigationWorkflowRecord record, CancellationToken cancellationToken = default);

    /// <summary>Obtém um workflow de mitigação pelo seu identificador único.</summary>
    Task<MitigationWorkflowRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todos os workflows de mitigação associados a um incidente,
    /// ordenados por data de criação descendente.
    /// </summary>
    Task<IReadOnlyList<MitigationWorkflowRecord>> GetByIncidentIdAsync(
        string incidentId, CancellationToken cancellationToken = default);
}
