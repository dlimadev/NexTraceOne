using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.FinOps.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetFinOpsInsights;

/// <summary>
/// Feature: GetFinOpsInsights — gera insights de otimização FinOps por serviço e equipa.
///
/// Detecta padrões de desperdício e anomalias de custo:
/// - Serviços com custo acima do P75 (outliers por equipa/domínio)
/// - Serviços em ambiente não-produtivo com custo superior ao ambiente de produção
/// - Categorias de custo com crescimento superior a 20% entre os últimos dois períodos
///
/// Adequado para Executive, Platform Admin e FinOps persona views.
///
/// Wave I.2 — FinOps Contextual por Serviço (OperationalIntelligence).
/// </summary>
public static class GetFinOpsInsights
{
    public sealed record Query(
        string TenantId,
        int Days = 30) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Days).InclusiveBetween(1, 365);
        }
    }

    public sealed class Handler(
        IServiceCostAllocationRepository repository,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;
            var since = now.AddDays(-request.Days);
            var previousSince = since.AddDays(-request.Days);

            // Registos do período atual e do período anterior para comparação de crescimento
            var currentRecords = await repository.ListByTenantAsync(request.TenantId, since, now, ct: cancellationToken);
            var previousRecords = await repository.ListByTenantAsync(request.TenantId, previousSince, since, ct: cancellationToken);

            var insights = new List<FinOpsInsight>();

            // ── Insight 1: Outliers de custo (acima do P75) ──────────────────────
            var serviceTotals = currentRecords
                .GroupBy(r => r.ServiceName)
                .Select(g => (ServiceName: g.Key, Total: g.Sum(r => r.AmountUsd)))
                .ToList();

            if (serviceTotals.Count > 1)
            {
                var sorted = serviceTotals.Select(s => s.Total).OrderBy(v => v).ToList();
                var p75Index = (int)Math.Ceiling(sorted.Count * 0.75) - 1;
                var p75 = sorted[Math.Max(0, p75Index)];

                foreach (var item in serviceTotals.Where(s => s.Total > p75))
                {
                    insights.Add(new FinOpsInsight(
                        InsightType: FinOpsInsightType.CostOutlier,
                        ServiceName: item.ServiceName,
                        Category: null,
                        CurrentAmountUsd: Math.Round(item.Total, 2),
                        PreviousAmountUsd: null,
                        GrowthPercent: null,
                        Description: $"Service '{item.ServiceName}' cost (${Math.Round(item.Total, 2)}) is above the P75 threshold (${Math.Round(p75, 2)}) for the period."));
                }
            }

            // ── Insight 2: Não-produção com custo > produção ──────────────────────
            var prodCosts = currentRecords
                .Where(r => r.Environment.Equals("production", StringComparison.OrdinalIgnoreCase) ||
                             r.Environment.Equals("prod", StringComparison.OrdinalIgnoreCase))
                .GroupBy(r => r.ServiceName)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.AmountUsd));

            var nonProdCosts = currentRecords
                .Where(r => !r.Environment.Equals("production", StringComparison.OrdinalIgnoreCase) &&
                             !r.Environment.Equals("prod", StringComparison.OrdinalIgnoreCase))
                .GroupBy(r => r.ServiceName)
                .Select(g => new { ServiceName = g.Key, Total = g.Sum(r => r.AmountUsd), Env = g.First().Environment });

            foreach (var item in nonProdCosts)
            {
                if (prodCosts.TryGetValue(item.ServiceName, out var prodTotal) && item.Total > prodTotal)
                {
                    insights.Add(new FinOpsInsight(
                        InsightType: FinOpsInsightType.NonProdExceedsProd,
                        ServiceName: item.ServiceName,
                        Category: null,
                        CurrentAmountUsd: Math.Round(item.Total, 2),
                        PreviousAmountUsd: Math.Round(prodTotal, 2),
                        GrowthPercent: null,
                        Description: $"Service '{item.ServiceName}' non-production cost (${Math.Round(item.Total, 2)} in {item.Env}) exceeds production cost (${Math.Round(prodTotal, 2)})."));
                }
            }

            // ── Insight 3: Categorias com crescimento > 20% ──────────────────────
            var currentByCategory = currentRecords
                .GroupBy(r => r.Category)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.AmountUsd));

            var previousByCategory = previousRecords
                .GroupBy(r => r.Category)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.AmountUsd));

            foreach (var kvp in currentByCategory)
            {
                var category = kvp.Key;
                var currentTotal = kvp.Value;
                if (!previousByCategory.TryGetValue(category, out var previousTotal) || previousTotal == 0)
                    continue;

                var growth = (currentTotal - previousTotal) / previousTotal * 100m;
                if (growth > 20m)
                {
                    insights.Add(new FinOpsInsight(
                        InsightType: FinOpsInsightType.CategoryGrowth,
                        ServiceName: null,
                        Category: category,
                        CurrentAmountUsd: Math.Round(currentTotal, 2),
                        PreviousAmountUsd: Math.Round(previousTotal, 2),
                        GrowthPercent: Math.Round(growth, 1),
                        Description: $"Category '{category}' cost grew {Math.Round(growth, 1)}% vs. prior period (${Math.Round(previousTotal, 2)} → ${Math.Round(currentTotal, 2)})."));
                }
            }

            var grandTotal = Math.Round(currentRecords.Sum(r => r.AmountUsd), 2);

            return Result<Response>.Success(new Response(
                GeneratedAt: now,
                PeriodDays: request.Days,
                TenantId: request.TenantId,
                TotalAmountUsd: grandTotal,
                TotalInsights: insights.Count,
                Insights: insights));
        }
    }

    public enum FinOpsInsightType
    {
        CostOutlier = 0,
        NonProdExceedsProd = 1,
        CategoryGrowth = 2,
    }

    public sealed record FinOpsInsight(
        FinOpsInsightType InsightType,
        string? ServiceName,
        CostCategory? Category,
        decimal CurrentAmountUsd,
        decimal? PreviousAmountUsd,
        decimal? GrowthPercent,
        string Description);

    public sealed record Response(
        DateTimeOffset GeneratedAt,
        int PeriodDays,
        string TenantId,
        decimal TotalAmountUsd,
        int TotalInsights,
        IReadOnlyList<FinOpsInsight> Insights);
}
