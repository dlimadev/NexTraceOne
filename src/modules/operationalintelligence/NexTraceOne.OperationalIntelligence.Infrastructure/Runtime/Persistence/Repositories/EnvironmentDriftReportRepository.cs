using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para relatórios de drift entre ambientes (EnvironmentDriftReport).
/// Isolamento total: acessa apenas RuntimeIntelligenceDbContext — sem acesso cross-module.
/// </summary>
internal sealed class EnvironmentDriftReportRepository(RuntimeIntelligenceDbContext context)
    : RepositoryBase<EnvironmentDriftReport, EnvironmentDriftReportId>(context), IEnvironmentDriftReportRepository
{
    /// <summary>Obtém um relatório pelo identificador.</summary>
    public override async Task<EnvironmentDriftReport?> GetByIdAsync(EnvironmentDriftReportId id, CancellationToken ct = default)
        => await context.EnvironmentDriftReports
            .SingleOrDefaultAsync(r => r.Id == id, ct);

    /// <summary>Lista relatórios, opcionalmente filtrados por ambientes e/ou status.</summary>
    public async Task<IReadOnlyList<EnvironmentDriftReport>> ListAsync(
        string? sourceEnvironment,
        string? targetEnvironment,
        DriftReportStatus? status,
        CancellationToken ct)
    {
        var query = context.EnvironmentDriftReports.AsQueryable();

        if (sourceEnvironment is not null)
            query = query.Where(r => r.SourceEnvironment == sourceEnvironment);

        if (targetEnvironment is not null)
            query = query.Where(r => r.TargetEnvironment == targetEnvironment);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync(ct);
    }

    /// <summary>Obtém o relatório mais recente para um par de ambientes.</summary>
    public async Task<EnvironmentDriftReport?> GetLatestAsync(
        string sourceEnvironment,
        string targetEnvironment,
        CancellationToken ct)
        => await context.EnvironmentDriftReports
            .Where(r => r.SourceEnvironment == sourceEnvironment && r.TargetEnvironment == targetEnvironment)
            .OrderByDescending(r => r.GeneratedAt)
            .FirstOrDefaultAsync(ct);

    /// <summary>Adiciona um novo relatório.</summary>
    public new void Add(EnvironmentDriftReport report)
    {
        context.EnvironmentDriftReports.Add(report);
    }

    /// <summary>Atualiza um relatório existente.</summary>
    public new void Update(EnvironmentDriftReport report)
    {
        context.EnvironmentDriftReports.Update(report);
    }
}
