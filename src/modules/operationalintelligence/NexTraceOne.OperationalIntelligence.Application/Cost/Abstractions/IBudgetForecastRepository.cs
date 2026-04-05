using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

/// <summary>
/// Contrato de repositório para a entidade BudgetForecast.
/// </summary>
public interface IBudgetForecastRepository
{
    Task<BudgetForecast?> GetLatestByServiceAsync(string serviceId, string environment, CancellationToken ct = default);
    Task<IReadOnlyList<BudgetForecast>> ListByServiceAsync(string serviceId, string environment, CancellationToken ct = default);
    void Add(BudgetForecast forecast);
}
