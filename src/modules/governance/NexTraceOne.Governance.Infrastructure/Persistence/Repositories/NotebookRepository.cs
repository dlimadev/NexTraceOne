using Microsoft.EntityFrameworkCore;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de Notebook (V3.4).
/// </summary>
internal sealed class NotebookRepository(GovernanceDbContext context) : INotebookRepository
{
    public async Task<IReadOnlyList<Notebook>> ListAsync(
        string tenantId,
        string? persona,
        NotebookStatus? status,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = context.Notebooks
            .Where(n => n.TenantId == tenantId);

        if (persona is not null)
            query = query.Where(n => n.Persona == persona);

        if (status.HasValue)
            query = query.Where(n => n.Status == status.Value);

        return await query
            .OrderByDescending(n => n.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<int> CountAsync(
        string tenantId,
        string? persona,
        NotebookStatus? status,
        CancellationToken ct)
    {
        var query = context.Notebooks.Where(n => n.TenantId == tenantId);
        if (persona is not null) query = query.Where(n => n.Persona == persona);
        if (status.HasValue) query = query.Where(n => n.Status == status.Value);
        return await query.CountAsync(ct);
    }

    public async Task<Notebook?> GetByIdAsync(NotebookId id, string tenantId, CancellationToken ct)
        => await context.Notebooks
            .Where(n => n.Id == id && n.TenantId == tenantId)
            .SingleOrDefaultAsync(ct);

    public async Task AddAsync(Notebook notebook, CancellationToken ct)
        => await context.Notebooks.AddAsync(notebook, ct);

    public Task UpdateAsync(Notebook notebook, CancellationToken ct)
    {
        context.Notebooks.Update(notebook);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Notebook notebook, CancellationToken ct)
    {
        context.Notebooks.Remove(notebook);
        return Task.CompletedTask;
    }
}
