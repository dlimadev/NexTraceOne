using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para relatórios de resiliência (ResilienceReport).
/// Isolamento total: acessa apenas RuntimeIntelligenceDbContext — sem acesso cross-module.
/// </summary>
internal sealed class ResilienceReportRepository(RuntimeIntelligenceDbContext context)
    : RepositoryBase<ResilienceReport, ResilienceReportId>(context), IResilienceReportRepository
{
    /// <summary>Obtém um relatório pelo identificador.</summary>
    public override async Task<ResilienceReport?> GetByIdAsync(ResilienceReportId id, CancellationToken ct = default)
        => await context.ResilienceReports
            .SingleOrDefaultAsync(r => r.Id == id, ct);

    /// <summary>Obtém relatórios associados a um experimento de chaos específico.</summary>
    public async Task<IReadOnlyList<ResilienceReport>> GetByExperimentIdAsync(Guid experimentId, CancellationToken ct)
        => await context.ResilienceReports
            .Where(r => r.ChaosExperimentId == experimentId)
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync(ct);

    /// <summary>Lista relatórios, opcionalmente filtrados por nome de serviço.</summary>
    public async Task<IReadOnlyList<ResilienceReport>> ListByServiceAsync(string? serviceName, CancellationToken ct)
    {
        var query = context.ResilienceReports.AsQueryable();

        if (!string.IsNullOrWhiteSpace(serviceName))
            query = query.Where(r => r.ServiceName == serviceName);

        return await query
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync(ct);
    }

    /// <summary>Adiciona um novo relatório.</summary>
    public async Task AddAsync(ResilienceReport report, CancellationToken ct)
        => await context.ResilienceReports.AddAsync(report, ct);
}
