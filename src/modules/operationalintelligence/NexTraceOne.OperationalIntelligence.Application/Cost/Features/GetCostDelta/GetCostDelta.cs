using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.CostIntelligence.Application.Abstractions;
using NexTraceOne.CostIntelligence.Domain.Errors;

namespace NexTraceOne.CostIntelligence.Application.Features.GetCostDelta;

/// <summary>
/// Feature: GetCostDelta — compara custos entre dois períodos para análise de variação.
/// Calcula delta absoluto e percentual entre período corrente e período anterior,
/// permitindo identificar tendências de aumento ou redução de custos após deploys.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetCostDelta
{
    /// <summary>Query para comparar custos entre dois períodos de um serviço.</summary>
    public sealed record Query(
        string ServiceName,
        string Environment,
        DateTimeOffset CurrentPeriodStart,
        DateTimeOffset CurrentPeriodEnd,
        DateTimeOffset PreviousPeriodStart,
        DateTimeOffset PreviousPeriodEnd) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta de delta de custo entre períodos.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.CurrentPeriodStart).NotEmpty();
            RuleFor(x => x.CurrentPeriodEnd).NotEmpty()
                .GreaterThan(x => x.CurrentPeriodStart)
                .WithMessage("Current period end must be after current period start.");
            RuleFor(x => x.PreviousPeriodStart).NotEmpty();
            RuleFor(x => x.PreviousPeriodEnd).NotEmpty()
                .GreaterThan(x => x.PreviousPeriodStart)
                .WithMessage("Previous period end must be after previous period start.");
        }
    }

    /// <summary>
    /// Handler que calcula a variação de custo entre dois períodos.
    /// Busca snapshots de cada período, calcula médias e retorna o delta absoluto e percentual.
    /// </summary>
    public sealed class Handler(
        ICostSnapshotRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var currentSnapshots = await repository.ListByServiceAsync(
                request.ServiceName,
                request.Environment,
                1,
                1000,
                cancellationToken);

            var previousSnapshots = await repository.ListByServiceAsync(
                request.ServiceName,
                request.Environment,
                1,
                1000,
                cancellationToken);

            // Filtra snapshots por período
            var currentPeriodSnapshots = currentSnapshots
                .Where(s => s.CapturedAt >= request.CurrentPeriodStart && s.CapturedAt <= request.CurrentPeriodEnd)
                .ToList();

            var previousPeriodSnapshots = previousSnapshots
                .Where(s => s.CapturedAt >= request.PreviousPeriodStart && s.CapturedAt <= request.PreviousPeriodEnd)
                .ToList();

            var currentTotal = currentPeriodSnapshots.Sum(s => s.TotalCost);
            var previousTotal = previousPeriodSnapshots.Sum(s => s.TotalCost);

            var absoluteDelta = currentTotal - previousTotal;
            var percentageDelta = previousTotal != 0
                ? (absoluteDelta / previousTotal) * 100m
                : currentTotal != 0 ? 100m : 0m;

            return new Response(
                request.ServiceName,
                request.Environment,
                currentTotal,
                previousTotal,
                absoluteDelta,
                Math.Round(percentageDelta, 2),
                currentPeriodSnapshots.Count,
                previousPeriodSnapshots.Count);
        }
    }

    /// <summary>Resposta com análise de delta de custo entre dois períodos.</summary>
    public sealed record Response(
        string ServiceName,
        string Environment,
        decimal CurrentPeriodCost,
        decimal PreviousPeriodCost,
        decimal AbsoluteDelta,
        decimal PercentageDelta,
        int CurrentPeriodDataPoints,
        int PreviousPeriodDataPoints);
}
