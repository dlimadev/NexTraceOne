using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.CostIntelligence.Application.Abstractions;

namespace NexTraceOne.CostIntelligence.Application.Features.GetCostByRoute;

/// <summary>
/// Feature: GetCostByRoute — obtém atribuições de custo filtradas por serviço e ambiente.
/// Permite análise granular de custos por API/rota específica, com paginação.
/// Útil para identificar APIs de alto custo e otimizar alocação de recursos.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetCostByRoute
{
    /// <summary>Query para obter custo atribuído a um serviço/ambiente específico.</summary>
    public sealed record Query(
        string ServiceName,
        string Environment,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta de custo por rota/serviço.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>
    /// Handler que consulta atribuições de custo por serviço e ambiente.
    /// Retorna dados paginados com custo total, contagem de requisições e custo por requisição.
    /// </summary>
    public sealed class Handler(
        ICostAttributionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var attributions = await repository.ListByServiceAsync(
                request.ServiceName,
                request.Environment,
                request.Page,
                request.PageSize,
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
                request.ServiceName,
                request.Environment,
                items,
                totalCost,
                request.Page,
                request.PageSize);
        }
    }

    /// <summary>Resposta com atribuições de custo filtradas por serviço e ambiente.</summary>
    public sealed record Response(
        string ServiceName,
        string Environment,
        IReadOnlyList<CostAttributionItem> Attributions,
        decimal TotalCost,
        int Page,
        int PageSize);

    /// <summary>Item individual de atribuição de custo por rota.</summary>
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
