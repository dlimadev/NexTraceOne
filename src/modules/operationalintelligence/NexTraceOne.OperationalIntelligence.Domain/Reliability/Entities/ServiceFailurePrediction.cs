using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Errors;

namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

/// <summary>
/// Previsão de falha de serviço computada a partir de taxa de erros,
/// histórico de incidentes e frequência de mudanças.
/// </summary>
public sealed class ServiceFailurePrediction : Entity<ServiceFailurePredictionId>
{
    private ServiceFailurePrediction() { }

    /// <summary>Identificador do serviço avaliado.</summary>
    public string ServiceId { get; private set; } = string.Empty;

    /// <summary>Nome do serviço avaliado.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Ambiente ao qual a previsão se aplica.</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Probabilidade de falha em percentagem (0–100).</summary>
    public decimal FailureProbabilityPercent { get; private set; }

    /// <summary>Nível de risco: Low, Medium ou High.</summary>
    public string RiskLevel { get; private set; } = string.Empty;

    /// <summary>Horizonte de previsão: 24h, 48h ou 7d.</summary>
    public string PredictionHorizon { get; private set; } = string.Empty;

    /// <summary>Fatores causais que contribuíram para a previsão (JSONB, máx. 5).</summary>
    public IReadOnlyList<string> CausalFactors { get; private set; } = [];

    /// <summary>Ação recomendada com base no nível de risco.</summary>
    public string? RecommendedAction { get; private set; }

    /// <summary>Timestamp de quando a previsão foi computada.</summary>
    public DateTimeOffset ComputedAt { get; private set; }

    private static readonly string[] ValidHorizons = ["24h", "48h", "7d"];

    /// <summary>Cria uma nova previsão de falha de serviço.</summary>
    public static Result<ServiceFailurePrediction> Create(
        string serviceId,
        string serviceName,
        string environment,
        decimal failureProbabilityPercent,
        string predictionHorizon,
        IReadOnlyList<string> causalFactors,
        string? recommendedAction,
        DateTimeOffset computedAt)
    {
        if (string.IsNullOrWhiteSpace(serviceId) || serviceId.Length > 200)
            return Error.Validation("INVALID_SERVICE_ID", "ServiceId is required and must not exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(serviceName) || serviceName.Length > 200)
            return Error.Validation("INVALID_SERVICE_NAME", "ServiceName is required and must not exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(environment) || environment.Length > 100)
            return Error.Validation("INVALID_ENVIRONMENT", "Environment is required and must not exceed 100 characters.");
        if (failureProbabilityPercent < 0m || failureProbabilityPercent > 100m)
            return PredictiveIntelligenceErrors.InvalidProbability();
        if (!ValidHorizons.Contains(predictionHorizon))
            return PredictiveIntelligenceErrors.InvalidPredictionHorizon();
        if (causalFactors.Count > 5)
            return PredictiveIntelligenceErrors.TooManyCausalFactors();

        return Result<ServiceFailurePrediction>.Success(new ServiceFailurePrediction
        {
            Id = ServiceFailurePredictionId.New(),
            ServiceId = serviceId,
            ServiceName = serviceName,
            Environment = environment,
            FailureProbabilityPercent = failureProbabilityPercent,
            RiskLevel = ComputeRiskLevel(failureProbabilityPercent),
            PredictionHorizon = predictionHorizon,
            CausalFactors = causalFactors,
            RecommendedAction = recommendedAction,
            ComputedAt = computedAt
        });
    }

    /// <summary>Computa o nível de risco a partir da probabilidade de falha.</summary>
    public static string ComputeRiskLevel(decimal probability) =>
        probability < 20m ? "Low" : probability <= 50m ? "Medium" : "High";
}

/// <summary>Identificador fortemente tipado de ServiceFailurePrediction.</summary>
public sealed record ServiceFailurePredictionId(Guid Value) : TypedIdBase(Value)
{
    public static ServiceFailurePredictionId New() => new(Guid.NewGuid());
    public static ServiceFailurePredictionId From(Guid id) => new(id);
}
