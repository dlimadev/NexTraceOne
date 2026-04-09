using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;

/// <summary>
/// Interface do repositório de IncidentPredictionPattern.
/// Define operações para padrões preditivos de incidentes.
/// </summary>
public interface IIncidentPredictionPatternRepository
{
    Task<IncidentPredictionPattern?> GetByIdAsync(IncidentPredictionPatternId id, CancellationToken ct);
    Task<IReadOnlyList<IncidentPredictionPattern>> ListAsync(
        string? environment,
        PredictionPatternStatus? status,
        PredictionPatternType? patternType,
        CancellationToken ct);
    Task<IncidentPredictionPattern?> GetLatestByServiceAsync(string serviceId, string environment, CancellationToken ct);
    void Add(IncidentPredictionPattern pattern);
    void Update(IncidentPredictionPattern pattern);
}
