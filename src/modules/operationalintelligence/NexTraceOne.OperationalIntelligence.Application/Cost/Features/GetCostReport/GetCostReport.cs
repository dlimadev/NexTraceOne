using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.CostIntelligence.Application.Abstractions;

namespace NexTraceOne.CostIntelligence.Application.Features.GetCostReport;

/// <summary>
/// Feature: GetCostReport — obtém relatório de snapshots de custo para um serviço/ambiente.
/// Retorna lista paginada de snapshots capturados, permitindo análise histórica
/// de custos por serviço e ambiente com suporte a filtro por período.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetCostReport
{
    /// <summary>Query para obter relatório de custo de um serviço e ambiente.</summary>
    public sealed record Query(
        string ServiceName,
        string Environment,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta do relatório de custo.</summary>
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
    /// Handler que consulta snapshots de custo de um serviço/ambiente com paginação.
    /// Retorna os snapshots ordenados por data de captura descendente.
    /// </summary>
    public sealed class Handler(
        ICostSnapshotRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var snapshots = await repository.ListByServiceAsync(
                request.ServiceName,
                request.Environment,
                request.Page,
                request.PageSize,
                cancellationToken);

            var items = snapshots.Select(s => new CostSnapshotItem(
                s.Id.Value,
                s.ServiceName,
                s.Environment,
                s.TotalCost,
                s.CpuCostShare,
                s.MemoryCostShare,
                s.NetworkCostShare,
                s.StorageCostShare,
                s.Currency,
                s.CapturedAt,
                s.Source,
                s.Period)).ToList();

            return new Response(
                request.ServiceName,
                request.Environment,
                items,
                request.Page,
                request.PageSize);
        }
    }

    /// <summary>Resposta do relatório de custo com lista paginada de snapshots.</summary>
    public sealed record Response(
        string ServiceName,
        string Environment,
        IReadOnlyList<CostSnapshotItem> Snapshots,
        int Page,
        int PageSize);

    /// <summary>Item individual de snapshot de custo no relatório.</summary>
    public sealed record CostSnapshotItem(
        Guid SnapshotId,
        string ServiceName,
        string Environment,
        decimal TotalCost,
        decimal CpuCostShare,
        decimal MemoryCostShare,
        decimal NetworkCostShare,
        decimal StorageCostShare,
        string Currency,
        DateTimeOffset CapturedAt,
        string Source,
        string Period);
}
