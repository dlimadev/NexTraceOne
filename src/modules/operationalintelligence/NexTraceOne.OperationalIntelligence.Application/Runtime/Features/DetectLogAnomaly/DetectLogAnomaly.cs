using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.DetectLogAnomaly;

/// <summary>
/// Feature: DetectLogAnomaly — detecta anomalias em logs comparando contagem de erros
/// com hora anterior e baseline. Identifica pós-deploy anomalias quando ChangeId está presente.
/// Computação pura, sem persistência.
/// </summary>
public static class DetectLogAnomaly
{
    public sealed record Command(
        string ServiceId,
        string Environment,
        int ErrorCountLastHour,
        int ErrorCountPreviousHour,
        int WarningCountLastHour,
        decimal BaselineErrorRate,
        string? ChangeId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ErrorCountLastHour).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ErrorCountPreviousHour).GreaterThanOrEqualTo(0);
            RuleFor(x => x.WarningCountLastHour).GreaterThanOrEqualTo(0);
            RuleFor(x => x.BaselineErrorRate).GreaterThanOrEqualTo(0m);
        }
    }

    public sealed class Handler(IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var errorSpike = (request.ErrorCountLastHour - request.ErrorCountPreviousHour)
                / (decimal)Math.Max(request.ErrorCountPreviousHour, 1) * 100m;

            var isAnomaly = errorSpike > 50m
                || (request.ErrorCountLastHour > 0 && request.ErrorCountLastHour > request.BaselineErrorRate * 3m);

            var anomalyType = !isAnomaly ? "None"
                : errorSpike > 50m && request.ErrorCountPreviousHour > 0 ? "ErrorSpike"
                : !string.IsNullOrWhiteSpace(request.ChangeId) ? "Regression"
                : request.ErrorCountLastHour > request.BaselineErrorRate * 3m ? "BaselineDeviation"
                : "ErrorSpike";

            var postChangeAnomaly = !string.IsNullOrWhiteSpace(request.ChangeId) && isAnomaly;

            string? recommendation = isAnomaly
                ? postChangeAnomaly
                    ? $"Anomaly detected after change {request.ChangeId}. Consider rollback if error rate does not stabilize."
                    : "Error spike detected. Investigate recent deployments and infrastructure changes."
                : null;

            return Task.FromResult(Result<Response>.Success(new Response(
                request.ServiceId,
                isAnomaly,
                anomalyType,
                errorSpike,
                postChangeAnomaly,
                request.ChangeId,
                recommendation,
                clock.UtcNow)));
        }
    }

    public sealed record Response(
        string ServiceId,
        bool IsAnomaly,
        string AnomalyType,
        decimal ErrorSpikePercent,
        bool PostChangeAnomaly,
        string? ChangeId,
        string? Recommendation,
        DateTimeOffset DetectedAt);
}
