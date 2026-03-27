using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Repositories;

/// <summary>
/// Repositório de análises de tendência de custo, implementando consultas específicas de negócio.
/// Isolamento total: acessa apenas CostIntelligenceDbContext — sem acesso cross-module.
/// </summary>
internal sealed class CostTrendRepository(CostIntelligenceDbContext context)
    : RepositoryBase<CostTrend, CostTrendId>(context), ICostTrendRepository
{
    /// <summary>Busca uma análise de tendência de custo pelo seu identificador.</summary>
    public override async Task<CostTrend?> GetByIdAsync(CostTrendId id, CancellationToken ct = default)
        => await context.CostTrends
            .SingleOrDefaultAsync(t => t.Id == id, ct);

    /// <summary>Lista tendências de custo de um serviço e ambiente, ordenadas por início do período descendente.</summary>
    public async Task<IReadOnlyList<CostTrend>> ListByServiceAsync(
        string serviceName,
        string environment,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await context.CostTrends
            .Where(t => t.ServiceName == serviceName && t.Environment == environment)
            .OrderByDescending(t => t.PeriodStart)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
}
