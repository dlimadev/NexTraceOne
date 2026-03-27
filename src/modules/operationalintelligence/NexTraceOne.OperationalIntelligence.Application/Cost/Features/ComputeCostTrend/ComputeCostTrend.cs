using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.ComputeCostTrend;

/// <summary>
/// Feature: ComputeCostTrend — calcula tendência de custo de um serviço a partir de snapshots.
/// Agrega dados estatísticos (média diária, pico, variação percentual) e classifica
/// a direção da tendência (Rising, Stable, Declining) para alertas proativos.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ComputeCostTrend
{
    /// <summary>Comando para calcular a tendência de custo de um serviço num período.</summary>
    public sealed record Command(
        string ServiceName,
        string Environment,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de cálculo de tendência de custo.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.PeriodStart).NotEmpty();
            RuleFor(x => x.PeriodEnd).NotEmpty()
                .GreaterThan(x => x.PeriodStart)
                .WithMessage("Period end must be after period start.");
        }
    }

    /// <summary>
    /// Handler que calcula a tendência de custo a partir dos snapshots existentes.
    /// Busca snapshots no período, calcula estatísticas e persiste a entidade CostTrend.
    /// P6.3: corrigido — CostTrend agora é persistido via ICostTrendRepository.
    /// </summary>
    public sealed class Handler(
        ICostSnapshotRepository snapshotRepository,
        ICostTrendRepository trendRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var snapshots = await snapshotRepository.ListByServiceAsync(
                request.ServiceName,
                request.Environment,
                1,
                1000,
                cancellationToken);

            // Filtra snapshots dentro do período solicitado
            var periodSnapshots = snapshots
                .Where(s => s.CapturedAt >= request.PeriodStart && s.CapturedAt <= request.PeriodEnd)
                .OrderBy(s => s.CapturedAt)
                .ToList();

            var dataPointCount = periodSnapshots.Count;
            if (dataPointCount == 0)
                dataPointCount = 1;

            var totalDays = (request.PeriodEnd - request.PeriodStart).TotalDays;
            if (totalDays <= 0)
                totalDays = 1;

            var totalCost = periodSnapshots.Sum(s => s.TotalCost);
            var averageDailyCost = totalCost / (decimal)totalDays;
            var peakDailyCost = periodSnapshots.Count > 0
                ? periodSnapshots.Max(s => s.TotalCost)
                : 0m;

            // Calcula variação percentual comparando primeira e segunda metade do período
            var midPoint = request.PeriodStart + TimeSpan.FromDays(totalDays / 2);
            var firstHalf = periodSnapshots.Where(s => s.CapturedAt < midPoint).Sum(s => s.TotalCost);
            var secondHalf = periodSnapshots.Where(s => s.CapturedAt >= midPoint).Sum(s => s.TotalCost);
            var percentageChange = firstHalf != 0
                ? ((secondHalf - firstHalf) / firstHalf) * 100m
                : secondHalf != 0 ? 100m : 0m;

            var trendResult = CostTrend.Create(
                request.ServiceName,
                request.Environment,
                request.PeriodStart,
                request.PeriodEnd,
                Math.Round(averageDailyCost, 4),
                peakDailyCost,
                Math.Round(percentageChange, 2),
                dataPointCount);

            if (trendResult.IsFailure)
                return trendResult.Error;

            var trend = trendResult.Value;

            trendRepository.Add(trend);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                trend.Id.Value,
                trend.ServiceName,
                trend.Environment,
                trend.PeriodStart,
                trend.PeriodEnd,
                trend.AverageDailyCost,
                trend.PeakDailyCost,
                trend.TrendDirection.ToString(),
                trend.PercentageChange,
                trend.DataPointCount);
        }
    }

    /// <summary>Resposta do cálculo de tendência de custo com dados estatísticos.</summary>
    public sealed record Response(
        Guid TrendId,
        string ServiceName,
        string Environment,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        decimal AverageDailyCost,
        decimal PeakDailyCost,
        string TrendDirection,
        decimal PercentageChange,
        int DataPointCount);
}
