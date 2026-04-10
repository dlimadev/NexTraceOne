using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de ContractComplianceResult usando EF Core.
/// </summary>
internal sealed class ContractComplianceResultRepository(ContractsDbContext context)
    : IContractComplianceResultRepository
{
    public async Task<ContractComplianceResult?> GetByIdAsync(
        ContractComplianceResultId id, CancellationToken ct)
        => await context.ContractComplianceResults.SingleOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<ContractComplianceResult>> ListByGateAsync(
        Guid gateId, CancellationToken ct)
        => await context.ContractComplianceResults
            .Where(r => r.GateId == gateId)
            .OrderByDescending(r => r.EvaluatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ContractComplianceResult>> ListByContractVersionAsync(
        string contractVersionId, CancellationToken ct)
        => await context.ContractComplianceResults
            .Where(r => r.ContractVersionId == contractVersionId)
            .OrderByDescending(r => r.EvaluatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task AddAsync(ContractComplianceResult result, CancellationToken ct)
        => await context.ContractComplianceResults.AddAsync(result, ct);
}
