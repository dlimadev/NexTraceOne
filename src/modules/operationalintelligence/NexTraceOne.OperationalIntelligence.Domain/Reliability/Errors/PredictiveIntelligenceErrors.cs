using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Errors;

/// <summary>Erros do domínio de Predictive Intelligence.</summary>
public static class PredictiveIntelligenceErrors
{
    public static Error InvalidProbability() =>
        Error.Validation("INVALID_PROBABILITY", "Probability must be between 0 and 100.");

    public static Error TooManyCausalFactors() =>
        Error.Validation("TOO_MANY_CAUSAL_FACTORS", "Cannot specify more than 5 causal factors.");

    public static Error ServiceNotFound(string id) =>
        Error.NotFound("SERVICE_NOT_FOUND", $"Service '{id}' not found for prediction.");

    public static Error InvalidPredictionHorizon() =>
        Error.Validation("INVALID_PREDICTION_HORIZON", "Valid horizons: 24h, 48h, 7d.");
}
