using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostRecordsByService;

/// <summary>
/// Feature: GetCostRecordsByService — lista registos de custo importados para um serviço específico.
/// Permite ao frontend FinOps por serviço mostrar custos reais importados por período.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetCostRecordsByService
{
    public sealed record Query(
        string ServiceId,
        string? Period = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Period).MaximumLength(20).When(x => x.Period is not null);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public sealed class Handler(
        ICostRecordRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var records = await repository.ListByServiceAsync(
                request.ServiceId,
                request.Period,
                cancellationToken);

            // Apply pagination in memory (repository returns all records for the service/period)
            var paged = records
                .OrderByDescending(r => r.RecordedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var items = paged.Select(r => new CostRecordItem(
                r.Id.Value,
                r.ServiceId,
                r.ServiceName,
                r.Team,
                r.Domain,
                r.Environment,
                r.Period,
                r.TotalCost,
                r.Currency,
                r.Source,
                r.RecordedAt)).ToList();

            var totalCost = records.Sum(r => r.TotalCost);

            return Result<Response>.Success(new Response(
                request.ServiceId,
                request.Period,
                items,
                totalCost,
                records.Count,
                request.Page,
                request.PageSize));
        }
    }

    public sealed record CostRecordItem(
        Guid RecordId,
        string ServiceId,
        string ServiceName,
        string? Team,
        string? Domain,
        string? Environment,
        string Period,
        decimal TotalCost,
        string Currency,
        string Source,
        DateTimeOffset RecordedAt);

    public sealed record Response(
        string ServiceId,
        string? Period,
        IReadOnlyList<CostRecordItem> Items,
        decimal TotalCost,
        int TotalCount,
        int Page,
        int PageSize);
}
