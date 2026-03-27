using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostRecordsByRelease;

/// <summary>
/// Feature: GetCostRecordsByRelease — lista registos de custo correlacionados a uma release.
/// Permite ao frontend e engenheiros compreender o impacto financeiro de uma mudança específica.
/// Diferente de GetCostByRelease (que usa CostAttribution), esta feature usa CostRecord
/// com correlação directa via ReleaseId persistido.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetCostRecordsByRelease
{
    public sealed record Query(
        Guid ReleaseId,
        int Page = 1,
        int PageSize = 50) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public sealed class Handler(
        ICostRecordRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var records = await repository.ListByReleaseAsync(request.ReleaseId, cancellationToken);

            if (records.Count == 0)
                return CostIntelligenceErrors.NoRecordsForRelease(request.ReleaseId);

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
                r.RecordedAt)).ToList();

            var totalCost = records.Sum(r => r.TotalCost);

            return Result<Response>.Success(new Response(
                request.ReleaseId,
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
        Guid ReleaseId,
        IReadOnlyList<CostRecordItem> Items,
        decimal TotalCost,
        int TotalCount,
        int Page,
        int PageSize);
}
