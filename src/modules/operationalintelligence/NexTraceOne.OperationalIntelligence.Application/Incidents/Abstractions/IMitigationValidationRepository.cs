using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

/// <summary>
/// Repositório para persistência e leitura de MitigationValidationLog.
/// Separa a responsabilidade de validações de mitigação do IIncidentStore genérico.
/// </summary>
public interface IMitigationValidationRepository
{
    /// <summary>Persiste um novo registo de validação pós-mitigação.</summary>
    Task AddAsync(MitigationValidationLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todos os registos de validação associados a um workflow de mitigação,
    /// ordenados por data de validação descendente.
    /// </summary>
    Task<IReadOnlyList<MitigationValidationLog>> GetByWorkflowIdAsync(
        Guid workflowId, CancellationToken cancellationToken = default);
}
