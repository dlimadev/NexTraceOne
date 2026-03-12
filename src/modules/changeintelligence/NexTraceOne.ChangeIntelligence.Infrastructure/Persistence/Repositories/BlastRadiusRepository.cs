using Microsoft.EntityFrameworkCore;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;

namespace NexTraceOne.ChangeIntelligence.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de relatórios de blast radius, implementando consultas específicas de negócio.
/// </summary>
internal sealed class BlastRadiusRepository(ChangeIntelligenceDbContext context) : IBlastRadiusRepository
{
    /// <summary>Busca o relatório de blast radius de uma release.</summary>
    public async Task<BlastRadiusReport?> GetByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default)
        => await context.BlastRadiusReports
            .SingleOrDefaultAsync(r => r.ReleaseId == releaseId, cancellationToken);

    /// <summary>Adiciona um novo relatório de blast radius.</summary>
    public void Add(BlastRadiusReport report)
        => context.BlastRadiusReports.Add(report);
}
