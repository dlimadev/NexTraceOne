using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.PredictServiceFailure;

/// <summary>
/// Feature: PredictServiceFailure — calcula a probabilidade de falha de um serviço
/// a partir de fatores como taxa de erros, histórico de incidentes e frequência de mudanças.
/// </summary>
public static class PredictServiceFailure
{
    public sealed record Command(
        string ServiceId,
        string ServiceName,
        string Environment,
        string PredictionHorizon,
        decimal ErrorRatePercent,
        int IncidentCountLast30Days,
        decimal ChangeFrequencyScore,
        string? AdditionalContext) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.PredictionHorizon).Must(x => new[] { "24h", "48h", "7d" }.Contains(x))
                .WithMessage("Valid horizons: 24h, 48h, 7d.");
            RuleFor(x => x.ErrorRatePercent).InclusiveBetween(0m, 100m);
            RuleFor(x => x.IncidentCountLast30Days).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ChangeFrequencyScore).InclusiveBetween(0m, 10m);
        }
    }

    public sealed class Handler(
        IServiceFailurePredictionRepository repository,
        IReliabilityUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var errorWeight = request.ErrorRatePercent * 0.40m;
            var incidentWeight = Math.Min(request.IncidentCountLast30Days * 3m, 30m);
            var changeWeight = request.ChangeFrequencyScore * 3m;
            var probability = Math.Min(errorWeight + incidentWeight + changeWeight, 100m);

            var causalFactors = new List<string>();
            if (errorWeight > 5m) causalFactors.Add($"High error rate: {request.ErrorRatePercent:F1}%");
            if (incidentWeight > 5m) causalFactors.Add($"Recent incidents: {request.IncidentCountLast30Days} in 30 days");
            if (changeWeight > 5m) causalFactors.Add($"High change frequency score: {request.ChangeFrequencyScore:F1}");
            if (!string.IsNullOrWhiteSpace(request.AdditionalContext))
                causalFactors.Add(request.AdditionalContext);
            if (causalFactors.Count > 5) causalFactors = causalFactors.Take(5).ToList();

            var riskLevel = ServiceFailurePrediction.ComputeRiskLevel(probability);
            var recommendedAction = riskLevel switch
            {
                "High" => "Immediate action required: investigate error sources, review recent changes, and prepare rollback plan.",
                "Medium" => "Monitor closely: review error trends and schedule maintenance window.",
                _ => "No immediate action required: continue routine monitoring."
            };

            var result = ServiceFailurePrediction.Create(
                request.ServiceId,
                request.ServiceName,
                request.Environment,
                probability,
                request.PredictionHorizon,
                causalFactors,
                recommendedAction,
                clock.UtcNow);

            if (!result.IsSuccess) return result.Error;

            repository.Add(result.Value!);
            await unitOfWork.CommitAsync(cancellationToken);

            var prediction = result.Value!;
            return Result<Response>.Success(new Response(
                prediction.Id.Value,
                prediction.ServiceId,
                prediction.ServiceName,
                prediction.FailureProbabilityPercent,
                prediction.RiskLevel,
                prediction.PredictionHorizon,
                prediction.CausalFactors,
                prediction.RecommendedAction,
                prediction.ComputedAt));
        }
    }

    public sealed record Response(
        Guid PredictionId,
        string ServiceId,
        string ServiceName,
        decimal FailureProbabilityPercent,
        string RiskLevel,
        string PredictionHorizon,
        IReadOnlyList<string> CausalFactors,
        string? RecommendedAction,
        DateTimeOffset ComputedAt);
}
