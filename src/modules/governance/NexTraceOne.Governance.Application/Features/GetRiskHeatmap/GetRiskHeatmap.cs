using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using FluentValidation;

namespace NexTraceOne.Governance.Application.Features.GetRiskHeatmap;

/// <summary>
/// Feature: GetRiskHeatmap — heatmap de risco por categoria de Governance Pack.
/// Cada célula representa uma categoria de pack com risk score derivado de rollouts e waivers reais.
/// Dimensões cross-module (incidentes, regressões) retornam 0 — não disponíveis neste módulo.
/// </summary>
public static class GetRiskHeatmap
{
    /// <summary>Query de heatmap de risco. Dimensão: category (padrão) ou team/domain.</summary>
    public sealed record Query(
        string? Dimension = null) : IQuery<Response>;

    /// <summary>
    /// Handler que computa células do heatmap de risco a partir de dados reais de Governance Packs,
    /// Rollouts e Waivers. Cada categoria de pack gera uma célula com risk score real.
    /// </summary>
    /// <summary>Valida os filtros opcionais da query de heatmap de risco.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Dimension).MaximumLength(100)
                .When(x => x.Dimension is not null);
        }
    }

    public sealed class Handler(
        IGovernancePackRepository packRepository,
        IGovernanceWaiverRepository waiverRepository,
        IGovernanceRolloutRecordRepository rolloutRepository,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var dimension = request.Dimension ?? "category";

            var packs = await packRepository.ListAsync(category: null, status: null, ct: cancellationToken);
            var waivers = await waiverRepository.ListAsync(packId: null, status: null, ct: cancellationToken);
            var rollouts = await rolloutRepository.ListAsync(
                packId: null, scopeType: null, scopeValue: null, status: null, ct: cancellationToken);

            var waiversByPack = waivers.GroupBy(w => w.PackId).ToDictionary(g => g.Key, g => g.ToList());
            var rolloutsByPack = rollouts.GroupBy(r => r.PackId).ToDictionary(g => g.Key, g => g.ToList());

            // Agrupa packs por categoria e computa risk por categoria
            var cells = packs
                .GroupBy(p => p.Category)
                .Select(g => BuildCell(g.Key, g.ToList(), waiversByPack, rolloutsByPack))
                .OrderByDescending(c => c.RiskScore)
                .ToList();

            var response = new Response(
                Dimension: dimension,
                Cells: cells,
                GeneratedAt: clock.UtcNow);

            return Result<Response>.Success(response);
        }

        private static RiskHeatmapCellDto BuildCell(
            GovernanceRuleCategory category,
            IReadOnlyList<GovernancePack> categoryPacks,
            IReadOnlyDictionary<GovernancePackId, List<GovernanceWaiver>> waiversByPack,
            IReadOnlyDictionary<GovernancePackId, List<GovernanceRolloutRecord>> rolloutsByPack)
        {
            var totalPacks = categoryPacks.Count;
            var failedRollouts = 0;
            var pendingWaivers = 0;
            var approvedWaivers = 0;
            var draftPacks = 0;
            var hasReliabilityIssue = false;

            foreach (var pack in categoryPacks)
            {
                rolloutsByPack.TryGetValue(pack.Id, out var pr);
                waiversByPack.TryGetValue(pack.Id, out var pw);

                failedRollouts += (pr ?? []).Count(r => r.Status == RolloutStatus.Failed);
                pendingWaivers += (pw ?? []).Count(w => w.Status == WaiverStatus.Pending);
                approvedWaivers += (pw ?? []).Count(w => w.Status == WaiverStatus.Approved);

                if (pack.Status == GovernancePackStatus.Draft) draftPacks++;
                if ((pr ?? []).Any(r => r.Status == RolloutStatus.Failed)) hasReliabilityIssue = true;
            }

            // Risk score: 0-100, baseado em falhas reais de rollout e waivers pendentes
            var riskScore = Math.Min(100m, (failedRollouts * 30m) + (pendingWaivers * 15m) + (draftPacks * 5m));

            var riskLevel = failedRollouts > 0
                ? RiskLevel.Critical
                : pendingWaivers > 1
                    ? RiskLevel.High
                    : pendingWaivers > 0 || draftPacks > 0
                        ? RiskLevel.Medium
                        : RiskLevel.Low;

            var explanation = failedRollouts > 0
                ? $"{failedRollouts} failed rollout(s) in {category} packs require immediate action"
                : pendingWaivers > 0
                    ? $"{pendingWaivers} pending waiver(s) for {category} packs awaiting approval"
                    : draftPacks > 0
                        ? $"{draftPacks} {category} pack(s) still in draft — not yet published"
                        : $"All {totalPacks} {category} pack(s) are in healthy state";

            return new RiskHeatmapCellDto(
                GroupId: $"cat-{category.ToString().ToLowerInvariant()}",
                GroupName: category.ToString(),
                RiskLevel: riskLevel,
                RiskScore: riskScore,
                Incidents: 0,           // cross-module — não disponível neste módulo
                ChangeFailures: failedRollouts,
                ReliabilityDegradation: hasReliabilityIssue,
                ContractGaps: category == GovernanceRuleCategory.Contracts ? pendingWaivers : 0,
                DocumentationGaps: category == GovernanceRuleCategory.SourceOfTruth ? pendingWaivers : 0,
                RunbookGaps: category == GovernanceRuleCategory.Operations ? pendingWaivers : 0,
                DependencyFragility: 0, // cross-module — não disponível neste módulo
                RegressionCount: 0,     // cross-module — não disponível neste módulo
                Explanation: explanation);
        }
    }

    /// <summary>Resposta do heatmap de risco com células por grupo.</summary>
    public sealed record Response(
        string Dimension,
        IReadOnlyList<RiskHeatmapCellDto> Cells,
        DateTimeOffset GeneratedAt);

    /// <summary>Célula do heatmap de risco com indicadores multidimensionais e explicação.</summary>
    public sealed record RiskHeatmapCellDto(
        string GroupId,
        string GroupName,
        RiskLevel RiskLevel,
        decimal RiskScore,
        int Incidents,
        int ChangeFailures,
        bool ReliabilityDegradation,
        int ContractGaps,
        int DocumentationGaps,
        int RunbookGaps,
        int DependencyFragility,
        int RegressionCount,
        string Explanation);
}
