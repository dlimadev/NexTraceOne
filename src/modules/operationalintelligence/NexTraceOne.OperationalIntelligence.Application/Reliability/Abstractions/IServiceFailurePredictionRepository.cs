using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;

/// <summary>Contrato de repositório para ServiceFailurePrediction.</summary>
public interface IServiceFailurePredictionRepository
{
    Task<ServiceFailurePrediction?> GetByServiceAsync(string serviceId, string environment, string horizon, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceFailurePrediction>> ListAsync(string? environment = null, string? riskLevel = null, CancellationToken ct = default);
    void Add(ServiceFailurePrediction prediction);
}
