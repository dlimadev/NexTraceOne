using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.ListCostImportBatches;

/// <summary>
/// Feature: ListCostImportBatches — lista os batches de importação de custo com paginação.
/// Permite ao frontend e operadores auditarem o histórico de ingestão de dados de custo.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListCostImportBatches
{
    public sealed record Query(int Page = 1, int PageSize = 20) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public sealed class Handler(
        ICostImportBatchRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var batches = await repository.ListAsync(request.Page, request.PageSize, cancellationToken);

            var items = batches.Select(b => new BatchItem(
                b.Id.Value,
                b.Source,
                b.Period,
                b.Currency,
                b.RecordCount,
                b.Status,
                b.Error,
                b.ImportedAt)).ToList();

            return Result<Response>.Success(new Response(items, request.Page, request.PageSize));
        }
    }

    public sealed record BatchItem(
        Guid BatchId,
        string Source,
        string Period,
        string Currency,
        int RecordCount,
        string Status,
        string? Error,
        DateTimeOffset ImportedAt);

    public sealed record Response(
        IReadOnlyList<BatchItem> Items,
        int Page,
        int PageSize);
}
