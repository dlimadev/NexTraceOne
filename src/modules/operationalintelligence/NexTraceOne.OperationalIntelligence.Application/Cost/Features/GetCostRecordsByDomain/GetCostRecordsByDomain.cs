using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostRecordsByDomain;

/// <summary>
/// Feature: GetCostRecordsByDomain — lista e agrega registos de custo de um domínio de negócio.
/// Permite ao frontend Domain FinOps mostrar custos reais importados por domínio e período.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetCostRecordsByDomain
{
    public sealed record Query(
        string Domain,
        string? Period = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Domain).NotEmpty().MaximumLength(200);
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
            var records = await repository.ListByDomainAsync(request.Domain, request.Period, cancellationToken);

            var paged = records
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
                r.ReleaseId,
                r.RecordedAt)).ToList();

            var totalCost = records.Sum(r => r.TotalCost);

            return Result<Response>.Success(new Response(
                request.Domain,
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
        Guid? ReleaseId,
        DateTimeOffset RecordedAt);

    public sealed record Response(
        string Domain,
        string? Period,
        IReadOnlyList<CostRecordItem> Items,
        decimal TotalCost,
        int TotalCount,
        int Page,
        int PageSize);
}
