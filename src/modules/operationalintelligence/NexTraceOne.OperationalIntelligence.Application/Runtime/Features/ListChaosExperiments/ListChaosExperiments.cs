using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.ListChaosExperiments;

/// <summary>
/// Feature: ListChaosExperiments — lista experimentos de chaos engineering persistidos.
/// Consulta dados reais via IChaosExperimentRepository com filtros por serviço, ambiente e estado.
///
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListChaosExperiments
{
    /// <summary>Query para listar experimentos de chaos engineering com filtros opcionais e paginação.</summary>
    public sealed record Query(
        string? ServiceName,
        string? Environment,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros de listagem de experimentos de chaos.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>
    /// Handler que lista experimentos de chaos engineering persistidos,
    /// filtrados por tenant, serviço e ambiente.
    /// </summary>
    public sealed class Handler(
        IChaosExperimentRepository repository,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var experiments = await repository.ListAsync(
                currentTenant.Id.ToString(),
                request.ServiceName,
                request.Environment,
                status: null,
                cancellationToken);

            var paged = experiments
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(e => new ChaosExperimentSummary(
                    e.Id.Value,
                    e.ServiceName,
                    e.Environment,
                    e.ExperimentType,
                    e.RiskLevel,
                    e.Status.ToString(),
                    e.CreatedAt))
                .ToList();

            return Result<Response>.Success(new Response(paged, experiments.Count));
        }
    }

    /// <summary>Resumo de um experimento de chaos para listagem.</summary>
    public sealed record ChaosExperimentSummary(
        Guid ExperimentId,
        string ServiceName,
        string Environment,
        string ExperimentType,
        string RiskLevel,
        string Status,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta paginada com lista de experimentos de chaos.</summary>
    public sealed record Response(
        IReadOnlyList<ChaosExperimentSummary> Items,
        int TotalCount);
}
