using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;

/// <summary>Contrato de repositório para CapacityForecast.</summary>
public interface ICapacityForecastRepository
{
    Task<CapacityForecast?> GetByServiceAndResourceAsync(string serviceId, string environment, string resourceType, CancellationToken ct = default);
    Task<IReadOnlyList<CapacityForecast>> ListAsync(string? environment = null, string? saturationRisk = null, CancellationToken ct = default);
    void Add(CapacityForecast forecast);
}
