using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetImpactSimulation;

/// <summary>
/// Feature: GetImpactSimulation — obtém uma simulação de impacto por identificador.
/// Retorna o relatório completo com cenário, serviços afetados, consumidores, risco e mitigação.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GetImpactSimulation
{
    /// <summary>Query para obter uma simulação de impacto por Id.</summary>
    public sealed record Query(Guid ImpactSimulationId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ImpactSimulationId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que obtém a simulação de impacto por Id.
    /// </summary>
    public sealed class Handler(IImpactSimulationRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var simulation = await repository.GetByIdAsync(
                ImpactSimulationId.From(request.ImpactSimulationId), cancellationToken);

            if (simulation is null)
                return ContractsErrors.ImpactSimulationNotFound(request.ImpactSimulationId.ToString());

            return new Response(
                simulation.Id.Value,
                simulation.ServiceName,
                simulation.Scenario,
                simulation.ScenarioDescription,
                simulation.AffectedServices,
                simulation.BrokenConsumers,
                simulation.TransitiveCascadeDepth,
                simulation.RiskPercent,
                simulation.MitigationRecommendations,
                simulation.SimulatedAt,
                simulation.TenantId);
        }
    }

    /// <summary>Resposta completa de uma simulação de impacto.</summary>
    public sealed record Response(
        Guid ImpactSimulationId,
        string ServiceName,
        ImpactSimulationScenario Scenario,
        string ScenarioDescription,
        string? AffectedServices,
        string? BrokenConsumers,
        int TransitiveCascadeDepth,
        int RiskPercent,
        string? MitigationRecommendations,
        DateTimeOffset SimulatedAt,
        string? TenantId);
}
