using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

/// <summary>
/// Abstração de repositório para correlações dinâmicas incidente↔mudança.
/// Permite persistir e consultar os resultados do motor de correlação.
/// </summary>
public interface IIncidentCorrelationRepository
{
    /// <summary>Persiste uma nova correlação.</summary>
    Task AddAsync(IncidentChangeCorrelation correlation, CancellationToken cancellationToken);

    /// <summary>Persiste um conjunto de novas correlações num único commit (batch).</summary>
    Task AddRangeAsync(IReadOnlyList<IncidentChangeCorrelation> correlations, CancellationToken cancellationToken);

    /// <summary>Retorna todas as correlações de um incidente, ordenadas por nível de confiança (High primeiro).</summary>
    Task<IReadOnlyList<IncidentChangeCorrelation>> GetByIncidentIdAsync(
        Guid incidentId, CancellationToken cancellationToken);

    /// <summary>Verifica se já existe correlação para o par incidente+mudança (evita duplicados).</summary>
    Task<bool> ExistsByIncidentAndChangeAsync(
        Guid incidentId, Guid changeId, CancellationToken cancellationToken);
}
