using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiUsageDashboard;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

/// <summary>
/// Implementação nula de IAiUsageEntryRepository para ambientes sem ClickHouse configurado.
/// Retorna coleções vazias e ignora escritas. Não substitui a implementação real em produção.
/// </summary>
internal sealed class NullAiUsageEntryRepository : IAiUsageEntryRepository
{
    public Task AddAsync(AIUsageEntry entry, CancellationToken ct) => Task.CompletedTask;

    public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct)
        => Task.FromResult(0);

    public Task<IReadOnlyList<AIUsageEntry>> ListAsync(
        string? userId, Guid? modelId, DateTimeOffset? startDate, DateTimeOffset? endDate,
        UsageResult? result, AIClientType? clientType, int pageSize, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<AIUsageEntry>>(Array.Empty<AIUsageEntry>());

    public Task<IReadOnlyList<AiUsageAggregate>> GetAggregatedUsageAsync(
        Guid tenantId, DateTimeOffset start, DateTimeOffset end, string groupBy, int top, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<AiUsageAggregate>>(Array.Empty<AiUsageAggregate>());
}
