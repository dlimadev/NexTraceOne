using Microsoft.EntityFrameworkCore;

using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de runbooks operacionais.
/// Persiste e consulta entidades RunbookRecord no IncidentDbContext.
/// </summary>
internal sealed class EfRunbookRepository(IncidentDbContext context) : IRunbookRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RunbookRecord>> ListAsync(
        string? linkedService,
        string? linkedIncidentType,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = context.Runbooks
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(linkedService))
            query = query.Where(r =>
                r.LinkedService != null &&
                EF.Functions.ILike(r.LinkedService, $"%{linkedService}%"));

        if (!string.IsNullOrWhiteSpace(linkedIncidentType))
            query = query.Where(r =>
                r.LinkedIncidentType != null &&
                EF.Functions.ILike(r.LinkedIncidentType, linkedIncidentType));

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r =>
                EF.Functions.ILike(r.Title, $"%{search}%") ||
                EF.Functions.ILike(r.Description, $"%{search}%"));

        return await query
            .OrderByDescending(r => r.PublishedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RunbookRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Runbooks
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == RunbookRecordId.From(id), cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(RunbookRecord runbook, CancellationToken cancellationToken = default)
    {
        context.Runbooks.Add(runbook);
        await context.CommitAsync(cancellationToken);
    }
}
