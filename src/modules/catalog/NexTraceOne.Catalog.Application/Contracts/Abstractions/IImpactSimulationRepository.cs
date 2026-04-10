using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório para simulações de impacto de dependências entre serviços.
/// </summary>
public interface IImpactSimulationRepository
{
    /// <summary>Obtém uma simulação de impacto por identificador.</summary>
    Task<ImpactSimulation?> GetByIdAsync(ImpactSimulationId id, CancellationToken cancellationToken);

    /// <summary>Lista simulações de impacto por nome de serviço.</summary>
    Task<IReadOnlyList<ImpactSimulation>> ListByServiceAsync(string serviceName, CancellationToken cancellationToken);

    /// <summary>Lista simulações de impacto por tipo de cenário.</summary>
    Task<IReadOnlyList<ImpactSimulation>> ListByScenarioAsync(ImpactSimulationScenario scenario, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova simulação de impacto.</summary>
    Task AddAsync(ImpactSimulation simulation, CancellationToken cancellationToken);
}
