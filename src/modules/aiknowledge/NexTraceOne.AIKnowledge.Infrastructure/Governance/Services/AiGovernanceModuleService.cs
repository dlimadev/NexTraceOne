using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Contracts.Governance.ServiceInterfaces;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Services;

/// <summary>
/// Implementação do contrato <see cref="IAiGovernanceModule"/> que expõe métricas de tokens
/// e atribuição de modelo para consumo cross-module.
/// Consulta o <see cref="AiGovernanceDbContext"/> de forma read-only (AsNoTracking).
/// </summary>
internal sealed class AiGovernanceModuleService(
    AiGovernanceDbContext context) : IAiGovernanceModule
{
    public async Task<TokenUsageSummaryDto?> GetTokenUsageByExecutionIdAsync(
        string executionId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(executionId))
            return null;

        var usageRecords = await context.TokenUsageLedger
            .AsNoTracking()
            .Where(l => l.ExecutionId == executionId && l.Status == "Success")
            .Select(l => new { l.TotalTokens, l.ModelName, l.EstimatedCostUsd })
            .ToListAsync(ct);

        if (usageRecords.Count == 0)
            return null;

        var totalTokens = usageRecords.Sum(r => r.TotalTokens);
        var predominantModel = usageRecords
            .Where(r => !string.IsNullOrWhiteSpace(r.ModelName))
            .GroupBy(r => r.ModelName)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();
        var totalCost = usageRecords
            .Where(r => r.EstimatedCostUsd.HasValue)
            .Sum(r => r.EstimatedCostUsd ?? 0m);

        return new TokenUsageSummaryDto(
            TotalTokens: totalTokens,
            ModelName: predominantModel,
            EstimatedCostUsd: totalCost > 0 ? totalCost : null);
    }
}
