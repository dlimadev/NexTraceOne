using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Errors;

namespace NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

/// <summary>
/// Recomendação de eficiência gerada por análise comparativa de custo entre serviços similares.
/// </summary>
public sealed class EfficiencyRecommendation : Entity<EfficiencyRecommendationId>
{
    private EfficiencyRecommendation() { }

    public string ServiceId { get; private set; } = string.Empty;
    public string ServiceName { get; private set; } = string.Empty;
    public string Environment { get; private set; } = string.Empty;
    public decimal ServiceCost { get; private set; }
    public decimal MedianPeerCost { get; private set; }
    public decimal DeviationPercent { get; private set; }
    public string RecommendationText { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string Priority { get; private set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; private set; }
    public bool IsAcknowledged { get; private set; }

    public static Result<EfficiencyRecommendation> Create(
        string serviceId,
        string serviceName,
        string environment,
        decimal serviceCost,
        decimal medianPeerCost,
        string recommendationText,
        string category,
        DateTimeOffset generatedAt)
    {
        Guard.Against.NullOrWhiteSpace(serviceId);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.NullOrWhiteSpace(category);

        if (string.IsNullOrWhiteSpace(recommendationText) || recommendationText.Length > 2000)
            return CostIntelligenceErrors.InvalidRecommendationText();

        var deviationPercent = medianPeerCost > 0
            ? (serviceCost - medianPeerCost) / medianPeerCost * 100m
            : 0m;

        var priority = deviationPercent > 100 ? "High"
            : deviationPercent > 40 ? "Medium"
            : "Low";

        return new EfficiencyRecommendation
        {
            Id = EfficiencyRecommendationId.New(),
            ServiceId = serviceId,
            ServiceName = serviceName,
            Environment = environment,
            ServiceCost = serviceCost,
            MedianPeerCost = medianPeerCost,
            DeviationPercent = deviationPercent,
            RecommendationText = recommendationText,
            Category = category,
            Priority = priority,
            GeneratedAt = generatedAt,
            IsAcknowledged = false
        };
    }

    public void Acknowledge()
    {
        IsAcknowledged = true;
    }
}

/// <summary>Identificador fortemente tipado de EfficiencyRecommendation.</summary>
public sealed record EfficiencyRecommendationId(Guid Value) : TypedIdBase(Value)
{
    public static EfficiencyRecommendationId New() => new(Guid.NewGuid());
    public static EfficiencyRecommendationId From(Guid id) => new(id);
}
