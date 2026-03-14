using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.CostIntelligence.Application.Abstractions;
using NexTraceOne.CostIntelligence.Domain.Entities;

namespace NexTraceOne.CostIntelligence.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de atribuições de custo a serviços/APIs.
/// Implementa consultas de negócio para análise de custo por serviço e período.
/// </summary>
internal sealed class CostAttributionRepository(CostIntelligenceDbContext context)
    : RepositoryBase<CostAttribution, CostAttributionId>(context), ICostAttributionRepository
{
    /// <summary>Busca uma atribuição de custo pelo seu identificador.</summary>
    public override async Task<CostAttribution?> GetByIdAsync(CostAttributionId id, CancellationToken ct = default)
        => await context.CostAttributions
            .SingleOrDefaultAsync(a => a.Id == id, ct);

    /// <summary>Lista atribuições de custo de um serviço e ambiente, ordenadas por período descendente.</summary>
    public async Task<IReadOnlyList<CostAttribution>> ListByServiceAsync(
        string serviceName,
        string environment,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await context.CostAttributions
            .Where(a => a.ServiceName == serviceName && a.Environment == environment)
            .OrderByDescending(a => a.PeriodEnd)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    /// <summary>Lista atribuições de custo dentro de um período específico.</summary>
    public async Task<IReadOnlyList<CostAttribution>> ListByPeriodAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken = default)
        => await context.CostAttributions
            .Where(a => a.PeriodStart >= periodStart && a.PeriodEnd <= periodEnd)
            .OrderByDescending(a => a.TotalCost)
            .ToListAsync(cancellationToken);
}
