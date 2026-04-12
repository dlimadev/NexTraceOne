using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ListImpactSimulations;

/// <summary>
/// Feature: ListImpactSimulations — lista simulações de impacto por nome de serviço.
/// Permite consultar o histórico de simulações what-if executadas para um serviço.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class ListImpactSimulations
{
    /// <summary>Query de listagem de simulações de impacto por serviço.</summary>
    public sealed record Query(string ServiceName) : IQuery<Response>;

    /// <summary>Valida a entrada da query de listagem.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que lista simulações de impacto por nome de serviço.
    /// </summary>
    public sealed class Handler(IImpactSimulationRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var items = await repository.ListByServiceAsync(request.ServiceName, cancellationToken);

            var simulations = items.Select(s => new ImpactSimulationListItem(
                s.Id.Value,
                s.ServiceName,
                s.Scenario,
                s.ScenarioDescription,
                s.TransitiveCascadeDepth,
                s.RiskPercent,
                s.SimulatedAt)).ToList();

            return new Response(simulations, simulations.Count);
        }
    }

    /// <summary>Item resumido de uma simulação de impacto.</summary>
    public sealed record ImpactSimulationListItem(
        Guid ImpactSimulationId,
        string ServiceName,
        ImpactSimulationScenario Scenario,
        string ScenarioDescription,
        int TransitiveCascadeDepth,
        int RiskPercent,
        DateTimeOffset SimulatedAt);

    /// <summary>Resposta da listagem de simulações de impacto.</summary>
    public sealed record Response(
        IReadOnlyList<ImpactSimulationListItem> Items,
        int TotalCount);
}
