using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.SimulateDependencyImpact;

/// <summary>
/// Feature: SimulateDependencyImpact — cria uma simulação de impacto de dependências entre serviços.
/// Persiste o cenário what-if com serviços afetados, consumidores que quebrariam,
/// profundidade de cascata, nível de risco e recomendações de mitigação.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class SimulateDependencyImpact
{
    /// <summary>Comando para executar uma simulação de impacto de dependências.</summary>
    public sealed record Command(
        string ServiceName,
        ImpactSimulationScenario Scenario,
        string ScenarioDescription,
        string? AffectedServices,
        string? BrokenConsumers,
        int TransitiveCascadeDepth,
        int RiskPercent,
        string? MitigationRecommendations,
        string? TenantId = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de simulação de impacto.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Scenario).IsInEnum();
            RuleFor(x => x.ScenarioDescription).NotEmpty().MaximumLength(4000);
            RuleFor(x => x.TransitiveCascadeDepth).GreaterThanOrEqualTo(0);
            RuleFor(x => x.RiskPercent).InclusiveBetween(0, 100);
        }
    }

    /// <summary>
    /// Handler que cria e persiste uma simulação de impacto de dependências.
    /// Delega a criação ao factory method ImpactSimulation.Simulate.
    /// </summary>
    public sealed class Handler(
        IImpactSimulationRepository repository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var simulation = ImpactSimulation.Simulate(
                request.ServiceName,
                request.Scenario,
                request.ScenarioDescription,
                request.AffectedServices,
                request.BrokenConsumers,
                request.TransitiveCascadeDepth,
                request.RiskPercent,
                request.MitigationRecommendations,
                dateTimeProvider.UtcNow,
                request.TenantId);

            await repository.AddAsync(simulation, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                simulation.Id.Value,
                simulation.ServiceName,
                simulation.Scenario,
                simulation.ScenarioDescription,
                simulation.TransitiveCascadeDepth,
                simulation.RiskPercent,
                simulation.SimulatedAt);
        }
    }

    /// <summary>Resposta da simulação de impacto de dependências.</summary>
    public sealed record Response(
        Guid ImpactSimulationId,
        string ServiceName,
        ImpactSimulationScenario Scenario,
        string ScenarioDescription,
        int TransitiveCascadeDepth,
        int RiskPercent,
        DateTimeOffset SimulatedAt);
}
