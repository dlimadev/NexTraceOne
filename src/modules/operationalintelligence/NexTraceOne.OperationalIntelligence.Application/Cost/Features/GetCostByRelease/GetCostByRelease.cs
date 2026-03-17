using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostByRelease;

/// <summary>
/// Feature: GetCostByRelease — obtém atribuições de custo relacionadas a um período de release.
/// Correlaciona custos de infraestrutura com releases para medir o impacto financeiro
/// de cada deploy. Utiliza o período da release para buscar atribuições correspondentes.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetCostByRelease
{
    /// <summary>Query para obter custos atribuídos a um período de release.</summary>
    public sealed record Query(
        Guid ReleaseId,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta de custo por release.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.PeriodStart).NotEmpty();
            RuleFor(x => x.PeriodEnd).NotEmpty()
                .GreaterThan(x => x.PeriodStart)
                .WithMessage("Period end must be after period start.");
        }
    }

    /// <summary>
    /// Handler que busca atribuições de custo no período da release informada.
    /// Permite correlacionar custo operacional com o impacto de cada deploy.
    /// </summary>
    public sealed class Handler(
        ICostAttributionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var attributions = await repository.ListByPeriodAsync(
                request.PeriodStart,
                request.PeriodEnd,
                cancellationToken);

            var items = attributions.Select(a => new CostAttributionItem(
                a.Id.Value,
                a.ApiAssetId,
                a.ServiceName,
                a.PeriodStart,
                a.PeriodEnd,
                a.TotalCost,
                a.RequestCount,
                a.CostPerRequest,
                a.Environment)).ToList();

            var totalCost = items.Sum(i => i.TotalCost);

            return new Response(
                request.ReleaseId,
                request.PeriodStart,
                request.PeriodEnd,
                items,
                totalCost);
        }
    }

    /// <summary>Resposta com atribuições de custo correlacionadas ao período da release.</summary>
    public sealed record Response(
        Guid ReleaseId,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        IReadOnlyList<CostAttributionItem> Attributions,
        decimal TotalCost);

    /// <summary>Item individual de atribuição de custo por release.</summary>
    public sealed record CostAttributionItem(
        Guid AttributionId,
        Guid ApiAssetId,
        string ServiceName,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        decimal TotalCost,
        long RequestCount,
        decimal CostPerRequest,
        string Environment);
}
