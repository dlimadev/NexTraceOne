using Microsoft.EntityFrameworkCore;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

internal sealed class FinOpsBudgetApprovalRepository(GovernanceDbContext context)
    : IFinOpsBudgetApprovalRepository
{
    public async Task<FinOpsBudgetApproval?> GetByIdAsync(Guid id, CancellationToken ct)
        => await context.FinOpsBudgetApprovals
            .SingleOrDefaultAsync(a => a.Id == new FinOpsBudgetApprovalId(id), ct);

    public async Task<IReadOnlyList<FinOpsBudgetApproval>> ListAsync(
        FinOpsBudgetApprovalStatus? status,
        string? serviceName,
        CancellationToken ct)
    {
        var query = context.FinOpsBudgetApprovals.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(serviceName))
            query = query.Where(a => a.ServiceName == serviceName);

        return await query
            .OrderByDescending(a => a.RequestedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(FinOpsBudgetApproval approval, CancellationToken ct)
        => await context.FinOpsBudgetApprovals.AddAsync(approval, ct);

    public void Update(FinOpsBudgetApproval approval)
        => context.FinOpsBudgetApprovals.Update(approval);
}
