using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetSloBurnRateAlert;

/// <summary>
/// Feature: GetSloBurnRateAlert — calcula o burn rate de SLO e determina alertas críticos
/// baseados na taxa de consumo do error budget. Não persiste dados — computação pura.
/// </summary>
public static class GetSloBurnRateAlert
{
    public sealed record Query(
        string ServiceId,
        string Environment,
        decimal CurrentErrorRatePercent,
        decimal SloTargetPercent,
        string WindowHours) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.CurrentErrorRatePercent).InclusiveBetween(0m, 100m);
            RuleFor(x => x.SloTargetPercent).InclusiveBetween(0m, 100m);
            RuleFor(x => x.WindowHours)
                .Must(x => new[] { "1", "6", "24", "72", "168" }.Contains(x))
                .WithMessage("Valid window hours: 1, 6, 24, 72, 168.");
        }
    }

    public sealed class Handler(IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var errorBudgetPercent = 100m - request.SloTargetPercent;
            var burnRate = request.CurrentErrorRatePercent / Math.Max(errorBudgetPercent, 0.001m);
            var windowH = int.Parse(request.WindowHours);
            decimal? timeToExhaustionHours = burnRate > 0m ? (decimal)windowH / burnRate : null;
            var isCritical = burnRate >= 14.4m;
            var isWarning = burnRate >= 1.0m && !isCritical;

            string? alertMessage = isCritical
                ? $"CRITICAL: SLO burn rate {burnRate:F1}× — budget exhaustion in {timeToExhaustionHours:F1}h"
                : isWarning
                    ? $"WARNING: SLO burn rate {burnRate:F1}× — monitor closely"
                    : $"OK: SLO burn rate {burnRate:F2}× — within safe limits";

            return Task.FromResult(Result<Response>.Success(new Response(
                request.ServiceId,
                request.Environment,
                request.CurrentErrorRatePercent,
                request.SloTargetPercent,
                errorBudgetPercent,
                burnRate,
                timeToExhaustionHours,
                isCritical,
                isWarning,
                alertMessage,
                clock.UtcNow)));
        }
    }

    public sealed record Response(
        string ServiceId,
        string Environment,
        decimal CurrentErrorRatePercent,
        decimal SloTargetPercent,
        decimal ErrorBudgetPercent,
        decimal BurnRate,
        decimal? TimeToExhaustionHours,
        bool IsCritical,
        bool IsWarning,
        string? AlertMessage,
        DateTimeOffset ComputedAt);
}
