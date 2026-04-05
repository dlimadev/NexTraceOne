using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.ForecastBudget;

public static class ForecastBudget
{
    public sealed record Command(
        string ServiceId,
        string ServiceName,
        string Environment,
        string ForecastPeriod) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ForecastPeriod).NotEmpty().MaximumLength(20);
        }
    }

    public sealed class Handler(
        IBudgetForecastRepository forecastRepository,
        IServiceCostProfileRepository profileRepository,
        ICostSnapshotRepository snapshotRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var profile = await profileRepository.GetByServiceAndEnvironmentAsync(
                request.ServiceId,
                request.Environment,
                cancellationToken);

            var snapshots = await snapshotRepository.ListByServiceAsync(
                request.ServiceId,
                request.Environment,
                1,
                6,
                cancellationToken);

            decimal projectedCost;
            decimal confidencePercent;
            string method;

            if (snapshots.Count < 2)
            {
                projectedCost = profile?.CurrentMonthCost ?? 0m;
                confidencePercent = 30m;
                method = "Insufficient data";
            }
            else
            {
                var ordered = snapshots.OrderByDescending(s => s.CapturedAt).Take(2).ToList();
                projectedCost = ordered.Average(s => s.TotalCost) * 1.05m;
                confidencePercent = Math.Min(90m, 50m + snapshots.Count * 5m);
                method = "LinearTrend";
            }

            var now = dateTimeProvider.UtcNow;

            var forecastResult = BudgetForecast.Create(
                request.ServiceId,
                request.ServiceName,
                request.Environment,
                request.ForecastPeriod,
                projectedCost,
                profile?.MonthlyBudget,
                confidencePercent,
                method,
                null,
                now);

            if (forecastResult.IsFailure)
                return forecastResult.Error;

            forecastRepository.Add(forecastResult.Value);
            await unitOfWork.CommitAsync(cancellationToken);

            var forecast = forecastResult.Value;

            return new Response(
                forecast.Id.Value,
                forecast.ServiceId,
                forecast.ForecastPeriod,
                forecast.ProjectedCost,
                forecast.BudgetLimit,
                forecast.ConfidencePercent,
                forecast.IsOverBudgetProjected,
                forecast.Method,
                forecast.ComputedAt);
        }
    }

    public sealed record Response(
        Guid ForecastId,
        string ServiceId,
        string ForecastPeriod,
        decimal ProjectedCost,
        decimal? BudgetLimit,
        decimal ConfidencePercent,
        bool IsOverBudgetProjected,
        string Method,
        DateTimeOffset ComputedAt);
}
