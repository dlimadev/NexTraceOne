using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetControlsSummary;

/// <summary>
/// Feature: GetControlsSummary — resumo de controles enterprise por dimensão.
/// Computa cobertura, maturidade e gaps a partir de dados reais de Governance Packs,
/// Rollouts e Waivers. Cada categoria de pack mapeia para uma dimensão de controle.
/// </summary>
public static class GetControlsSummary
{
    /// <summary>Query para resumo de controles enterprise. Filtrável por equipa, domínio ou serviço.</summary>
    public sealed record Query(
        string? TeamId = null,
        string? DomainId = null,
        string? ServiceId = null) : IQuery<Response>;

    /// <summary>
    /// Handler que computa controles enterprise a partir de dados reais de Governance Packs,
    /// Rollouts e Waivers. A cobertura de cada dimensão deriva da taxa de rollouts concluídos
    /// vs. packs da categoria correspondente.
    /// </summary>
    public sealed class Handler(
        IGovernancePackRepository packRepository,
        IGovernanceWaiverRepository waiverRepository,
        IGovernanceRolloutRecordRepository rolloutRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var packs = await packRepository.ListAsync(category: null, status: null, ct: cancellationToken);
            var waivers = await waiverRepository.ListAsync(packId: null, status: null, ct: cancellationToken);
            var rollouts = await rolloutRepository.ListAsync(
                packId: null, scopeType: null, scopeValue: null, status: null, ct: cancellationToken);

            var waiversByPack = waivers
                .GroupBy(w => w.PackId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var rolloutsByPack = rollouts
                .GroupBy(r => r.PackId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var dimensions = BuildDimensions(packs, waiversByPack, rolloutsByPack);

            var overallCoverage = dimensions.Count == 0 ? 0m : Math.Round(dimensions.Average(d => d.CoveragePercent), 1);
            var criticalGapCount = dimensions.Count(d => d.CoveragePercent < 60m);

            var overallMaturity = overallCoverage >= 90m
                ? MaturityLevel.Optimizing
                : overallCoverage >= 75m
                    ? MaturityLevel.Managed
                    : overallCoverage >= 55m
                        ? MaturityLevel.Defined
                        : overallCoverage >= 30m
                            ? MaturityLevel.Developing
                            : MaturityLevel.Initial;

            var response = new Response(
                OverallCoverage: overallCoverage,
                OverallMaturity: overallMaturity,
                TotalDimensions: dimensions.Count,
                CriticalGapCount: criticalGapCount,
                Dimensions: dimensions,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Result<Response>.Success(response);
        }

        private static IReadOnlyList<ControlDimensionDto> BuildDimensions(
            IReadOnlyList<GovernancePack> packs,
            IReadOnlyDictionary<GovernancePackId, List<GovernanceWaiver>> waiversByPack,
            IReadOnlyDictionary<GovernancePackId, List<GovernanceRolloutRecord>> rolloutsByPack)
        {
            // Mapeia categorias de pack para dimensões de controle enterprise
            var categoryToDimension = new Dictionary<GovernanceRuleCategory, ControlDimension>
            {
                [GovernanceRuleCategory.Contracts] = ControlDimension.ContractGovernance,
                [GovernanceRuleCategory.SourceOfTruth] = ControlDimension.SourceOfTruthCompleteness,
                [GovernanceRuleCategory.Changes] = ControlDimension.ChangeGovernance,
                [GovernanceRuleCategory.Incidents] = ControlDimension.IncidentMitigationEvidence,
                [GovernanceRuleCategory.AIGovernance] = ControlDimension.AiGovernance,
                [GovernanceRuleCategory.Operations] = ControlDimension.DocumentationRunbookReadiness,
                [GovernanceRuleCategory.Reliability] = ControlDimension.OwnershipCoverage,
            };

            var dimensions = new List<ControlDimensionDto>();

            foreach (var (category, dimension) in categoryToDimension)
            {
                var categoryPacks = packs.Where(p => p.Category == category).ToList();
                if (categoryPacks.Count == 0) continue;

                var totalAssessed = categoryPacks.Count;
                var completedCount = 0;
                var gapCount = 0;
                var trend = TrendDirection.Stable;

                foreach (var pack in categoryPacks)
                {
                    rolloutsByPack.TryGetValue(pack.Id, out var pr);
                    waiversByPack.TryGetValue(pack.Id, out var pw);

                    var hasCompletedRollout = (pr ?? []).Any(r => r.Status == RolloutStatus.Completed);
                    var hasPendingWaiver = (pw ?? []).Any(w => w.Status == WaiverStatus.Pending);
                    var hasFailedRollout = (pr ?? []).Any(r => r.Status == RolloutStatus.Failed);

                    if (hasCompletedRollout && !hasPendingWaiver)
                        completedCount++;
                    else
                        gapCount++;

                    if (hasFailedRollout)
                        trend = TrendDirection.Declining;
                }

                var coverage = totalAssessed == 0 ? 0m : Math.Round(((decimal)completedCount / totalAssessed) * 100m, 1);

                // Se não há rollouts ainda, pack está em análise inicial
                if (!rolloutsByPack.Keys.Any(k => categoryPacks.Select(p => p.Id).Contains(k)))
                    trend = TrendDirection.Stable;

                var maturity = coverage >= 90m
                    ? MaturityLevel.Optimizing
                    : coverage >= 75m
                        ? MaturityLevel.Managed
                        : coverage >= 55m
                            ? MaturityLevel.Defined
                            : coverage >= 30m
                                ? MaturityLevel.Developing
                                : MaturityLevel.Initial;

                var summary = gapCount == 0
                    ? $"All {totalAssessed} {category} pack(s) are compliant with no pending waivers"
                    : $"{gapCount} of {totalAssessed} {category} pack(s) have gaps (pending waivers or no completed rollout)";

                dimensions.Add(new ControlDimensionDto(
                    Dimension: dimension,
                    CoveragePercent: coverage,
                    TotalAssessed: totalAssessed,
                    GapCount: gapCount,
                    Maturity: maturity,
                    Trend: trend,
                    Summary: summary));
            }

            return dimensions.OrderByDescending(d => d.CoveragePercent).ToList();
        }
    }

    /// <summary>Resposta com resumo de controles enterprise.</summary>
    public sealed record Response(
        decimal OverallCoverage,
        MaturityLevel OverallMaturity,
        int TotalDimensions,
        int CriticalGapCount,
        IReadOnlyList<ControlDimensionDto> Dimensions,
        DateTimeOffset GeneratedAt);

    /// <summary>DTO de dimensão de controle enterprise.</summary>
    public sealed record ControlDimensionDto(
        ControlDimension Dimension,
        decimal CoveragePercent,
        int TotalAssessed,
        int GapCount,
        MaturityLevel Maturity,
        TrendDirection Trend,
        string Summary);
}
