using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para propostas de breaking change. CC-06.
/// </summary>
internal sealed class BreakingChangeProposalRepository(ContractsDbContext context)
    : IBreakingChangeProposalRepository
{
    public async Task AddAsync(BreakingChangeProposal proposal, CancellationToken ct)
        => await context.BreakingChangeProposals.AddAsync(proposal, ct);

    public async Task<BreakingChangeProposal?> GetByIdAsync(BreakingChangeProposalId id, CancellationToken ct)
        => await context.BreakingChangeProposals.SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<BreakingChangeProposal>> ListByContractAsync(
        Guid contractId, string tenantId, CancellationToken ct)
        => await context.BreakingChangeProposals
            .Where(p => p.ContractId == contractId && p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<BreakingChangeProposal>> ListActiveByTenantAsync(
        string tenantId, CancellationToken ct)
        => await context.BreakingChangeProposals
            .Where(p => p.TenantId == tenantId &&
                       p.Status != BreakingChangeProposalStatus.Approved &&
                       p.Status != BreakingChangeProposalStatus.Rejected)
            .OrderByDescending(p => p.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public Task UpdateAsync(BreakingChangeProposal proposal, CancellationToken ct)
    {
        context.BreakingChangeProposals.Update(proposal);
        return Task.CompletedTask;
    }
}
