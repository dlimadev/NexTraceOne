using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de simulações de impacto de dependências entre serviços.
/// Persiste e consulta cenários what-if com serviços afetados, consumidores, risco e mitigação.
/// </summary>
internal sealed class ImpactSimulationRepository(ContractsDbContext context)
    : IImpactSimulationRepository
{
    /// <inheritdoc />
    public async Task<ImpactSimulation?> GetByIdAsync(ImpactSimulationId id, CancellationToken cancellationToken)
        => await context.ImpactSimulations
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImpactSimulation>> ListByServiceAsync(string serviceName, CancellationToken cancellationToken)
        => await context.ImpactSimulations
            .AsNoTracking()
            .Where(x => x.ServiceName == serviceName)
            .OrderByDescending(x => x.SimulatedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImpactSimulation>> ListByScenarioAsync(ImpactSimulationScenario scenario, CancellationToken cancellationToken)
        => await context.ImpactSimulations
            .AsNoTracking()
            .Where(x => x.Scenario == scenario)
            .OrderByDescending(x => x.SimulatedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(ImpactSimulation simulation, CancellationToken cancellationToken)
        => await context.ImpactSimulations.AddAsync(simulation, cancellationToken);
}
