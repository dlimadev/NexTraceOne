using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiAgentPerformanceMetricRepository(AiGovernanceDbContext context)
    : IAiAgentPerformanceMetricRepository
{
    public async Task<IReadOnlyList<AiAgentPerformanceMetric>> ListByTenantAsync(
        Guid tenantId, CancellationToken ct)
        => await context.AgentPerformanceMetrics
            .Where(m => m.TenantId == tenantId)
            .OrderByDescending(m => m.PeriodStart)
            .ToListAsync(ct);

    public async Task<AiAgentPerformanceMetric?> GetByAgentAndPeriodAsync(
        AiAgentId agentId, DateTimeOffset periodStart, CancellationToken ct)
        => await context.AgentPerformanceMetrics
            .FirstOrDefaultAsync(m => m.AgentId == agentId && m.PeriodStart == periodStart, ct);

    public void Add(AiAgentPerformanceMetric metric)
        => context.AgentPerformanceMetrics.Add(metric);
}
