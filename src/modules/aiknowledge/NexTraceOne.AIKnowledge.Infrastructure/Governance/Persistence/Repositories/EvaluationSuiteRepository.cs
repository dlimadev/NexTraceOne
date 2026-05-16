using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class EvaluationSuiteRepository(AiGovernanceDbContext context, ICurrentTenant currentTenant) : IEvaluationSuiteRepository
{
    public void Add(EvaluationSuite suite)
        => context.EvaluationSuites.Add(suite);

    public async Task<EvaluationSuite?> GetByIdAsync(EvaluationSuiteId id, CancellationToken ct)
        => await context.EvaluationSuites.Where(e => e.TenantId == currentTenant.Id).SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<EvaluationSuite>> ListByTenantAsync(
        Guid tenantId, string? useCase, int page, int pageSize, CancellationToken ct)
    {
        var query = context.EvaluationSuites.Where(s => s.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(useCase))
            query = query.Where(s => s.UseCase == useCase);

        return await query
            .OrderByDescending(s => s.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountByTenantAsync(Guid tenantId, string? useCase, CancellationToken ct)
    {
        var query = context.EvaluationSuites.Where(s => s.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(useCase))
            query = query.Where(s => s.UseCase == useCase);

        return await query.CountAsync(ct);
    }
}
