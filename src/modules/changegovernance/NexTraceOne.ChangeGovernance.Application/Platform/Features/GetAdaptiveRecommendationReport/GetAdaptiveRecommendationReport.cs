using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Platform.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Platform.Features.GetAdaptiveRecommendationReport;

/// <summary>
/// Feature: GetAdaptiveRecommendationReport — motor de recomendações adaptativas cross-wave.
///
/// Prioriza as acções de maior impacto para melhorar governança, fiabilidade, segurança, qualidade e adopção.
/// A pontuação de prioridade é calculada como <c>ImpactScore / EffortMultiplier</c>.
///
/// Thresholds configuráveis via IConfigurationResolutionService:
/// - <c>platform.recommendations.top_n</c> (default 10) — número máximo de recomendações
/// - <c>platform.recommendations.refresh_cron</c> (default "0 6 * * *") — periodicidade de actualização
/// - <c>platform.recommendations.low_effort_sprints</c> (default 1) — sprints para itens de baixo esforço
///
/// Wave AU.3 — Platform Self-Optimization &amp; Adaptive Intelligence (ChangeGovernance Platform).
/// </summary>
public static class GetAdaptiveRecommendationReport
{
    // ── Configuration keys ─────────────────────────────────────────────────
    internal const string TopNKey = "platform.recommendations.top_n";
    internal const string RefreshCronKey = "platform.recommendations.refresh_cron";
    internal const string LowEffortSprintsKey = "platform.recommendations.low_effort_sprints";
    internal const int DefaultTopN = 10;
    internal const string DefaultRefreshCron = "0 6 * * *";
    internal const int DefaultLowEffortSprints = 1;

    // ── Effort multipliers ─────────────────────────────────────────────────
    internal const decimal LowEffortMultiplier = 1.0m;
    internal const decimal MediumEffortMultiplier = 2.0m;
    internal const decimal HighEffortMultiplier = 3.0m;

    private const int ActionPrioritySummaryCount = 3;

    // ── Enums ──────────────────────────────────────────────────────────────
    /// <summary>Categoria de recomendação adaptativa.</summary>
    public enum RecommendationCategory { Reliability, Security, Governance, Quality, Adoption }

    /// <summary>Estimativa de esforço para implementar a recomendação.</summary>
    public enum EffortEstimate { Low, Medium, High }

    // ── Query ──────────────────────────────────────────────────────────────
    /// <summary>Query para o relatório de recomendações adaptativas.</summary>
    public sealed record Query(
        string TenantId,
        int TopN = DefaultTopN) : IQuery<Report>;

    /// <summary>Validador da query <see cref="Query"/>.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TopN).InclusiveBetween(1, 50);
        }
    }

    // ── Value objects ──────────────────────────────────────────────────────
    /// <summary>Item de recomendação adaptativa priorizado.</summary>
    public sealed record RecommendationItem(
        Guid RecommendationId,
        RecommendationCategory Category,
        string Title,
        string Description,
        int ImpactScore,
        EffortEstimate EffortEstimate,
        decimal EffortMultiplier,
        IReadOnlyList<string> AffectedServices,
        IReadOnlyList<string> AffectedTeams,
        string RecommendationSource,
        IReadOnlyList<string> EvidenceLinks,
        decimal PriorityScore);

    /// <summary>Distribuição de recomendações por categoria.</summary>
    public sealed record CategoryDistributionItem(
        RecommendationCategory Category,
        int Count,
        decimal PctOfTotal);

    /// <summary>Relatório completo de recomendações adaptativas.</summary>
    public sealed record Report(
        string TenantId,
        IReadOnlyList<RecommendationItem> Top10Recommendations,
        IReadOnlyList<CategoryDistributionItem> CategoryDistribution,
        decimal RecommendationActionability,
        IReadOnlyList<string> TenantActionPrioritySummary,
        DateTimeOffset RefreshedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    /// <summary>Handler da query <see cref="Query"/>.</summary>
    public sealed class Handler(
        IAdaptiveRecommendationReader recommendationReader,
        IConfigurationResolutionService configService,
        IDateTimeProvider clock)
        : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;

            // Resolve config
            var topNCfg = await configService.ResolveEffectiveValueAsync(
                TopNKey, ConfigurationScope.System, null, cancellationToken);

            var topN = int.TryParse(topNCfg?.EffectiveValue, out var tn) ? tn : request.TopN;

            var signals = await recommendationReader.GetSignalsAsync(request.TenantId, cancellationToken);

            // Map signals to items with priority scores
            var items = signals
                .Select(s => MapToItem(s))
                .OrderByDescending(i => i.PriorityScore)
                .Take(topN)
                .ToList();

            // Category distribution over all signals (not just top N)
            var allItems = signals.Select(s => MapToItem(s)).ToList();
            var categoryDistribution = allItems
                .GroupBy(i => i.Category)
                .Select(g => new CategoryDistributionItem(
                    Category: g.Key,
                    Count: g.Count(),
                    PctOfTotal: allItems.Count == 0 ? 0m : Math.Round((decimal)g.Count() / allItems.Count * 100m, 2)))
                .OrderByDescending(c => c.Count)
                .ToList();

            var actionability = items.Count == 0
                ? 0m
                : Math.Round((decimal)items.Count(i => i.EffortEstimate is EffortEstimate.Low or EffortEstimate.Medium)
                             / items.Count * 100m, 2);

            var prioritySummary = items
                .Take(ActionPrioritySummaryCount)
                .Select(i => $"{i.Category}: {i.Title}")
                .ToList();

            return Result<Report>.Success(new Report(
                TenantId: request.TenantId,
                Top10Recommendations: items,
                CategoryDistribution: categoryDistribution,
                RecommendationActionability: actionability,
                TenantActionPrioritySummary: prioritySummary,
                RefreshedAt: now));
        }

        private static decimal GetEffortMultiplier(EffortEstimate effort) => effort switch
        {
            EffortEstimate.Low => LowEffortMultiplier,
            EffortEstimate.Medium => MediumEffortMultiplier,
            EffortEstimate.High => HighEffortMultiplier,
            _ => LowEffortMultiplier
        };

        private static RecommendationItem MapToItem(IAdaptiveRecommendationReader.RecommendationSignal s)
        {
            var multiplier = GetEffortMultiplier(s.EffortEstimate);
            var priorityScore = multiplier == 0 ? 0m : Math.Round((decimal)s.ImpactScore / multiplier, 2);
            return new RecommendationItem(
                RecommendationId: s.RecommendationId,
                Category: s.Category,
                Title: s.Title,
                Description: s.Description,
                ImpactScore: s.ImpactScore,
                EffortEstimate: s.EffortEstimate,
                EffortMultiplier: multiplier,
                AffectedServices: s.AffectedServices,
                AffectedTeams: s.AffectedTeams,
                RecommendationSource: s.RecommendationSource,
                EvidenceLinks: s.EvidenceLinks,
                PriorityScore: priorityScore);
        }
    }
}
