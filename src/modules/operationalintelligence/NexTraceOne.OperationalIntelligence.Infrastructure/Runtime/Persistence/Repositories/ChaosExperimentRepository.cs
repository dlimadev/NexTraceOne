using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para experimentos de chaos engineering.
/// Isolamento total: acessa apenas RuntimeIntelligenceDbContext — sem acesso cross-module.
/// </summary>
internal sealed class ChaosExperimentRepository(RuntimeIntelligenceDbContext context)
    : RepositoryBase<ChaosExperiment, ChaosExperimentId>(context), IChaosExperimentRepository
{
    /// <summary>Busca um experimento pelo seu identificador, filtrando por tenant.</summary>
    public async Task<ChaosExperiment?> GetByIdAsync(
        ChaosExperimentId id, string tenantId, CancellationToken cancellationToken)
        => await context.ChaosExperiments
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, cancellationToken);

    /// <summary>Lista experimentos com filtros opcionais, ordenados por data de criação descendente.</summary>
    public async Task<IReadOnlyList<ChaosExperiment>> ListAsync(
        string tenantId,
        string? serviceName,
        string? environment,
        ExperimentStatus? status,
        CancellationToken cancellationToken)
    {
        var query = context.ChaosExperiments
            .Where(e => e.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(serviceName))
            query = query.Where(e => e.ServiceName == serviceName);

        if (!string.IsNullOrWhiteSpace(environment))
            query = query.Where(e => e.Environment == environment);

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>Adiciona um novo experimento.</summary>
    public async Task AddAsync(ChaosExperiment experiment, CancellationToken cancellationToken)
        => await context.ChaosExperiments.AddAsync(experiment, cancellationToken);

    /// <summary>Atualiza um experimento existente.</summary>
    public Task UpdateAsync(ChaosExperiment experiment, CancellationToken cancellationToken)
    {
        context.ChaosExperiments.Update(experiment);
        return Task.CompletedTask;
    }
}
