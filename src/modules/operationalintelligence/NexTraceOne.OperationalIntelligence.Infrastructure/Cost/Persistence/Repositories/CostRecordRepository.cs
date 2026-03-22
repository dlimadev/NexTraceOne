using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Repositories;

/// <summary>
/// Repositório de registos de custo, implementando consultas específicas de negócio.
/// Isolamento total: acessa apenas CostIntelligenceDbContext — sem acesso cross-module.
/// </summary>
internal sealed class CostRecordRepository(CostIntelligenceDbContext context)
    : RepositoryBase<CostRecord, CostRecordId>(context), ICostRecordRepository
{
    /// <summary>Busca um registo de custo pelo seu identificador.</summary>
    public override async Task<CostRecord?> GetByIdAsync(CostRecordId id, CancellationToken ct = default)
        => await context.CostRecords
            .SingleOrDefaultAsync(r => r.Id == id, ct);

    /// <summary>Lista registos de custo por período, ordenados por custo total descendente.</summary>
    public async Task<IReadOnlyList<CostRecord>> ListByPeriodAsync(string period, CancellationToken cancellationToken = default)
        => await context.CostRecords
            .Where(r => r.Period == period)
            .OrderByDescending(r => r.TotalCost)
            .ToListAsync(cancellationToken);

    /// <summary>Lista registos de custo de um serviço específico, opcionalmente filtrado por período.</summary>
    public async Task<IReadOnlyList<CostRecord>> ListByServiceAsync(string serviceId, string? period = null, CancellationToken cancellationToken = default)
    {
        var query = context.CostRecords.Where(r => r.ServiceId == serviceId);

        if (!string.IsNullOrWhiteSpace(period))
            query = query.Where(r => r.Period == period);

        return await query
            .OrderByDescending(r => r.RecordedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Lista registos de custo de uma equipa, opcionalmente filtrado por período.</summary>
    public async Task<IReadOnlyList<CostRecord>> ListByTeamAsync(string team, string? period = null, CancellationToken cancellationToken = default)
    {
        var query = context.CostRecords.Where(r => r.Team == team);

        if (!string.IsNullOrWhiteSpace(period))
            query = query.Where(r => r.Period == period);

        return await query
            .OrderByDescending(r => r.TotalCost)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Lista registos de custo de um domínio, opcionalmente filtrado por período.</summary>
    public async Task<IReadOnlyList<CostRecord>> ListByDomainAsync(string domain, string? period = null, CancellationToken cancellationToken = default)
    {
        var query = context.CostRecords.Where(r => r.Domain == domain);

        if (!string.IsNullOrWhiteSpace(period))
            query = query.Where(r => r.Period == period);

        return await query
            .OrderByDescending(r => r.TotalCost)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Adiciona múltiplos registos de custo ao repositório em batch.</summary>
    public void AddRange(IEnumerable<CostRecord> records)
        => context.CostRecords.AddRange(records);
}
