using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Features.GetAiCostAttributionReport;

/// <summary>
/// Feature: GetAiCostAttributionReport — relatório de custo de IA por utilizador, provider e modelo.
/// Agrega o AiTokenUsageLedger para o período solicitado e calcula custo estimado.
/// Estrutura VSA: Query + Handler + Response.
/// </summary>
public static class GetAiCostAttributionReport
{
    /// <summary>Custo estimado por 1 000 tokens (USD, blended average).</summary>
    private const double CostPer1kTokensUsd = 0.002;

    public sealed record Query(
        Guid? TenantId = null,
        int DaysBack = 30,
        string? GroupBy = null) : IQuery<Response>;

    public sealed class Handler(
        IAiTokenUsageLedgerRepository ledgerRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-Math.Max(1, request.DaysBack));

            var entries = await ledgerRepository.ListByPeriodAsync(cutoff, cancellationToken);

            var byUser = entries
                .GroupBy(e => e.UserId)
                .Select(g => new UserAttribution(
                    UserId: g.Key,
                    TotalTokens: g.Sum(e => (long)e.TotalTokens),
                    EstimatedUsd: Math.Round(g.Sum(e => (long)e.TotalTokens) / 1000.0 * CostPer1kTokensUsd, 4),
                    Calls: g.Count()))
                .OrderByDescending(u => u.TotalTokens)
                .ToList();

            var byProvider = entries
                .GroupBy(e => e.ProviderId)
                .Select(g => new ProviderAttribution(
                    ProviderId: g.Key,
                    TotalTokens: g.Sum(e => (long)e.TotalTokens),
                    EstimatedUsd: Math.Round(g.Sum(e => (long)e.TotalTokens) / 1000.0 * CostPer1kTokensUsd, 4),
                    Calls: g.Count()))
                .OrderByDescending(p => p.TotalTokens)
                .ToList();

            var byModel = entries
                .GroupBy(e => e.ModelId)
                .Select(g => new ModelAttribution(
                    ModelId: g.Key,
                    ModelName: g.First().ModelName,
                    TotalTokens: g.Sum(e => (long)e.TotalTokens),
                    EstimatedUsd: Math.Round(g.Sum(e => (long)e.TotalTokens) / 1000.0 * CostPer1kTokensUsd, 4),
                    Calls: g.Count()))
                .OrderByDescending(m => m.TotalTokens)
                .ToList();

            var grandTotalTokens = entries.Sum(e => (long)e.TotalTokens);

            return new Response(
                PeriodFrom: cutoff,
                PeriodTo: DateTimeOffset.UtcNow,
                TotalEntries: entries.Count,
                GrandTotalTokens: grandTotalTokens,
                GrandTotalEstimatedUsd: Math.Round(grandTotalTokens / 1000.0 * CostPer1kTokensUsd, 4),
                ByUser: byUser,
                ByProvider: byProvider,
                ByModel: byModel);
        }
    }

    public sealed record UserAttribution(
        string UserId,
        long TotalTokens,
        double EstimatedUsd,
        int Calls);

    public sealed record ProviderAttribution(
        string ProviderId,
        long TotalTokens,
        double EstimatedUsd,
        int Calls);

    public sealed record ModelAttribution(
        string ModelId,
        string ModelName,
        long TotalTokens,
        double EstimatedUsd,
        int Calls);

    public sealed record Response(
        DateTimeOffset PeriodFrom,
        DateTimeOffset PeriodTo,
        int TotalEntries,
        long GrandTotalTokens,
        double GrandTotalEstimatedUsd,
        IReadOnlyList<UserAttribution> ByUser,
        IReadOnlyList<ProviderAttribution> ByProvider,
        IReadOnlyList<ModelAttribution> ByModel);
}
