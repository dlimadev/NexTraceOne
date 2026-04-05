using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Errors;

namespace NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

/// <summary>
/// Projeção de orçamento para um serviço baseada em tendência histórica e mudanças planeadas.
/// Permite identificar proativamente quando um serviço vai exceder orçamento.
/// </summary>
public sealed class BudgetForecast : Entity<BudgetForecastId>
{
    private BudgetForecast() { }

    public string ServiceId { get; private set; } = string.Empty;
    public string ServiceName { get; private set; } = string.Empty;
    public string Environment { get; private set; } = string.Empty;
    public string ForecastPeriod { get; private set; } = string.Empty;
    public decimal ProjectedCost { get; private set; }
    public decimal? BudgetLimit { get; private set; }
    public decimal ConfidencePercent { get; private set; }
    public bool IsOverBudgetProjected { get; private set; }
    public string? ForecastNotes { get; private set; }
    public DateTimeOffset ComputedAt { get; private set; }
    public string Method { get; private set; } = string.Empty;

    public static Result<BudgetForecast> Create(
        string serviceId,
        string serviceName,
        string environment,
        string forecastPeriod,
        decimal projectedCost,
        decimal? budgetLimit,
        decimal confidencePercent,
        string method,
        string? notes,
        DateTimeOffset computedAt)
    {
        Guard.Against.NullOrWhiteSpace(serviceId);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.NullOrWhiteSpace(method);

        if (projectedCost < 0)
            return CostIntelligenceErrors.NegativeCost(projectedCost);

        if (confidencePercent < 0 || confidencePercent > 100)
            return CostIntelligenceErrors.InvalidConfidencePercent();

        if (budgetLimit.HasValue && budgetLimit.Value <= 0)
            return CostIntelligenceErrors.NegativeCost(budgetLimit.Value);

        return new BudgetForecast
        {
            Id = BudgetForecastId.New(),
            ServiceId = serviceId,
            ServiceName = serviceName,
            Environment = environment,
            ForecastPeriod = forecastPeriod,
            ProjectedCost = projectedCost,
            BudgetLimit = budgetLimit,
            ConfidencePercent = confidencePercent,
            IsOverBudgetProjected = budgetLimit.HasValue && projectedCost > budgetLimit.Value,
            ForecastNotes = notes,
            ComputedAt = computedAt,
            Method = method
        };
    }
}

/// <summary>Identificador fortemente tipado de BudgetForecast.</summary>
public sealed record BudgetForecastId(Guid Value) : TypedIdBase(Value)
{
    public static BudgetForecastId New() => new(Guid.NewGuid());
    public static BudgetForecastId From(Guid id) => new(id);
}
