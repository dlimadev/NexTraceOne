using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de ContractComplianceGate usando EF Core.
/// </summary>
internal sealed class ContractComplianceGateRepository(ContractsDbContext context)
    : IContractComplianceGateRepository
{
    public async Task<ContractComplianceGate?> GetByIdAsync(
        ContractComplianceGateId id, CancellationToken ct)
        => await context.ContractComplianceGates.SingleOrDefaultAsync(g => g.Id == id, ct);

    public async Task<IReadOnlyList<ContractComplianceGate>> ListByScopeAsync(
        ComplianceGateScope scope, string scopeId, CancellationToken ct)
        => await context.ContractComplianceGates
            .Where(g => g.Scope == scope && g.ScopeId == scopeId)
            .OrderBy(g => g.Name)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ContractComplianceGate>> ListActiveAsync(CancellationToken ct)
        => await context.ContractComplianceGates
            .Where(g => g.IsActive)
            .OrderBy(g => g.Name)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task AddAsync(ContractComplianceGate gate, CancellationToken ct)
        => await context.ContractComplianceGates.AddAsync(gate, ct);
}
