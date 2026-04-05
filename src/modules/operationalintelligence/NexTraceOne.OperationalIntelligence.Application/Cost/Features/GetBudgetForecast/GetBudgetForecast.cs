using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetBudgetForecast;

public static class GetBudgetForecast
{
    public sealed record Query(string ServiceId, string Environment) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
        }
    }

    public sealed class Handler(IBudgetForecastRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var forecast = await repository.GetLatestByServiceAsync(
                request.ServiceId,
                request.Environment,
                cancellationToken);

            if (forecast is null)
                return CostIntelligenceErrors.ForecastNotFound(request.ServiceId);

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
