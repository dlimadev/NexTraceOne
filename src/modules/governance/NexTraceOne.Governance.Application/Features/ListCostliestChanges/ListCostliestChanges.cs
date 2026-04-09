using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.ListCostliestChanges;

/// <summary>
/// Feature: ListCostliestChanges — lista as N mudanças com maior impacto de custo.
/// Permite identificar rapidamente quais deploys tiveram maior impacto financeiro.
///
/// Owner: módulo Governance — subdomínio FinOps.
/// Pilar: FinOps contextual — ranking de mudanças por impacto de custo.
/// </summary>
public static class ListCostliestChanges
{
    /// <summary>Query para listar as mudanças mais custosas.</summary>
    public sealed record Query(int Top = 10) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Top).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    /// <summary>Handler que lista as mudanças com maior impacto de custo.</summary>
    public sealed class Handler(
        IChangeCostImpactRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var impacts = await repository.ListCostliestAsync(request.Top, cancellationToken);

            var items = impacts
                .Select(i => new ChangeCostItemDto(
                    ImpactId: i.Id.Value,
                    ReleaseId: i.ReleaseId,
                    ServiceName: i.ServiceName,
                    Environment: i.Environment,
                    CostDelta: i.CostDelta,
                    CostDeltaPercentage: i.CostDeltaPercentage,
                    Direction: i.Direction,
                    RecordedAt: i.RecordedAt))
                .ToList();

            return Result<Response>.Success(new Response(
                Items: items,
                TotalCount: items.Count));
        }
    }

    /// <summary>Resposta com a lista de mudanças mais custosas.</summary>
    public sealed record Response(
        IReadOnlyList<ChangeCostItemDto> Items,
        int TotalCount);

    /// <summary>DTO resumido de impacto de custo para listagem.</summary>
    public sealed record ChangeCostItemDto(
        Guid ImpactId,
        Guid ReleaseId,
        string ServiceName,
        string Environment,
        decimal CostDelta,
        decimal CostDeltaPercentage,
        CostChangeDirection Direction,
        DateTimeOffset RecordedAt);
}
