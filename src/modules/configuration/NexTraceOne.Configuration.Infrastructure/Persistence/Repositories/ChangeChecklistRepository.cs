using Microsoft.EntityFrameworkCore;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Repositories;

internal sealed class ChangeChecklistRepository(ConfigurationDbContext context) : IChangeChecklistRepository
{
    public async Task<ChangeChecklist?> GetByIdAsync(ChangeChecklistId id, string tenantId, CancellationToken cancellationToken)
        => await context.ChangeChecklists.SingleOrDefaultAsync(
            c => c.Id == id && c.TenantId == tenantId, cancellationToken);

    public async Task<IReadOnlyList<ChangeChecklist>> ListByTenantAsync(string tenantId, CancellationToken cancellationToken)
        => await context.ChangeChecklists
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ChangeChecklist>> GetForChangeAsync(string tenantId, string changeType, string? environment, CancellationToken cancellationToken)
        => await context.ChangeChecklists
            .Where(c => c.TenantId == tenantId
                && c.ChangeType == changeType
                && (environment == null || c.Environment == null || c.Environment == environment))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ChangeChecklist checklist, CancellationToken cancellationToken)
        => await context.ChangeChecklists.AddAsync(checklist, cancellationToken);

    public Task UpdateAsync(ChangeChecklist checklist, CancellationToken cancellationToken)
    {
        context.ChangeChecklists.Update(checklist);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(ChangeChecklistId id, CancellationToken cancellationToken)
    {
        var entity = await context.ChangeChecklists.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is not null) context.ChangeChecklists.Remove(entity);
    }
}
