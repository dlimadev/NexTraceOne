using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para playbooks operacionais.
/// Isolamento total: acessa apenas RuntimeIntelligenceDbContext — sem acesso cross-module.
/// </summary>
internal sealed class OperationalPlaybookRepository(RuntimeIntelligenceDbContext context)
    : RepositoryBase<OperationalPlaybook, OperationalPlaybookId>(context), IOperationalPlaybookRepository
{
    /// <summary>Obtém um playbook pelo identificador.</summary>
    public override async Task<OperationalPlaybook?> GetByIdAsync(OperationalPlaybookId id, CancellationToken ct = default)
        => await context.OperationalPlaybooks
            .SingleOrDefaultAsync(p => p.Id == id, ct);

    /// <summary>Lista playbooks do tenant, ordenados por data de criação descendente.</summary>
    public async Task<IReadOnlyList<OperationalPlaybook>> ListAsync(string tenantId, CancellationToken cancellationToken)
        => await context.OperationalPlaybooks
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    /// <summary>Lista playbooks filtrados por status.</summary>
    public async Task<IReadOnlyList<OperationalPlaybook>> ListByStatusAsync(
        string tenantId, PlaybookStatus status, CancellationToken cancellationToken)
        => await context.OperationalPlaybooks
            .Where(p => p.TenantId == tenantId && p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    /// <summary>Adiciona um novo playbook.</summary>
    public async Task AddAsync(OperationalPlaybook playbook, CancellationToken cancellationToken)
        => await context.OperationalPlaybooks.AddAsync(playbook, cancellationToken);
}
