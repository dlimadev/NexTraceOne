using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetCostPerReleaseReport;

/// <summary>
/// Feature: GetCostPerReleaseReport — custo operacional atribuído por release de serviço.
///
/// Responde "qual é o custo de deploy de uma release?" e identifica releases com cost spike
/// pós-deploy, correlacionando deploys falhados com custo desperdiçado.
///
/// Para cada release no período, calcula:
/// - <c>PreReleaseDailyAvgCostUsd</c>: custo médio diário nos N dias antes do deploy
/// - <c>PostReleaseDailyAvgCostUsd</c>: custo médio diário nos N dias após o deploy
/// - <c>CostDeltaPct</c>: variação percentual post vs. pre (positivo = aumento)
/// - <c>PostReleaseTotalCostUsd</c>: custo total no período de análise pós-deploy
///
/// <c>CostImpactTier</c>:
/// - <c>CostSaving</c>    — delta &lt; -10% (release reduziu custo)
/// - <c>Neutral</c>       — delta entre -10% e +10%
/// - <c>MinorIncrease</c> — delta 10%–30%
/// - <c>MajorIncrease</c> — delta 30%–100%
/// - <c>CostSpike</c>     — delta &gt; 100% (spike de custo pós-deploy)
///
/// Flag especial <c>WastedDeploymentCost</c> — deploy Failed/RolledBack com CostSpike.
///
/// Wave AG.2 — GetCostPerReleaseReport (OperationalIntelligence FinOps).
/// </summary>
public static class GetCostPerReleaseReport
{
    // ── Tier thresholds (CostDeltaPct) ─────────────────────────────────────
    private const double CostSavingThreshold = -10.0;
    private const double NeutralUpperThreshold = 10.0;
    private const double MinorIncreaseThreshold = 30.0;
    private const double MajorIncreaseThreshold = 100.0;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: período de análise para releases recentes (7–90, default 30).</para>
    /// <para><c>PreReleaseDays</c>: dias de baseline antes do deploy (3–30, default 7).</para>
    /// <para><c>PostReleaseDays</c>: dias de análise após o deploy (3–30, default 7).</para>
    /// <para><c>SpikeThresholdPct</c>: threshold % para CostSpike (50–500, default 100).</para>
    /// <para><c>TopReleasesCount</c>: máximo de releases com maior delta a listar (1–50, default 10).</para>
    /// <para><c>ServiceFilter</c>: filtro opcional por serviço.</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        int PreReleaseDays = 7,
        int PostReleaseDays = 7,
        double SpikeThresholdPct = 100.0,
        int TopReleasesCount = 10,
        string? ServiceFilter = null) : IQuery<Report>;

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>Impacto de custo de uma release comparado com baseline pré-deploy.</summary>
    public enum CostImpactTier
    {
        /// <summary>CostDeltaPct &lt; -10% — release reduziu custo operacional.</summary>
        CostSaving,
        /// <summary>CostDeltaPct entre -10% e +10% — impacto neutro no custo.</summary>
        Neutral,
        /// <summary>CostDeltaPct entre +10% e +30% — aumento menor de custo.</summary>
        MinorIncrease,
        /// <summary>CostDeltaPct entre +30% e SpikeThreshold — aumento significativo.</summary>
        MajorIncrease,
        /// <summary>CostDeltaPct &gt; SpikeThreshold — spike de custo pós-deploy.</summary>
        CostSpike
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Resumo de custo de uma release específica.</summary>
    public sealed record ReleaseWithCostEntry(
        string ReleaseId,
        string ServiceName,
        string Environment,
        DateTimeOffset DeployedAt,
        bool IsFailedOrRolledBack,
        decimal PreReleaseDailyAvgCostUsd,
        decimal PostReleaseDailyAvgCostUsd,
        decimal PostReleaseTotalCostUsd,
        double CostDeltaPct,
        CostImpactTier ImpactTier,
        bool WastedDeploymentCost);

    /// <summary>Sumário de custo de releases do tenant no período.</summary>
    public sealed record ReleaseCostSummary(
        int TotalReleases,
        decimal AvgCostPerRelease,
        int CostSpikeCount,
        int CostSavingCount,
        double CostSpikeRatePct,
        double CostSavingRatePct,
        decimal TotalWastedDeploymentCostUsd);

    /// <summary>Relatório de custo por release.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        int TotalReleasesAnalyzed,
        ReleaseCostSummary Summary,
        IReadOnlyList<ReleaseWithCostEntry> TopCostSpikeReleases,
        IReadOnlyList<ReleaseWithCostEntry> TopCostSavingReleases,
        IReadOnlyList<ReleaseWithCostEntry> AllReleases);

    // ── Reader abstraction ─────────────────────────────────────────────────

    /// <summary>Entrada de release com dados de custo pré e pós-deploy.</summary>
    public sealed record ReleaseCostData(
        string ReleaseId,
        string ServiceName,
        string Environment,
        DateTimeOffset DeployedAt,
        bool IsFailedOrRolledBack,
        decimal PreReleaseDailyAvgCostUsd,
        decimal PostReleaseDailyAvgCostUsd,
        decimal PostReleaseTotalCostUsd);

    /// <summary>Reader de dados de custo por release para o relatório AG.2.</summary>
    public interface ICostPerReleaseReader
    {
        /// <summary>Devolve dados de custo de releases no período.</summary>
        Task<IReadOnlyList<ReleaseCostData>> ListByTenantAsync(
            string tenantId,
            int lookbackDays,
            int preReleaseDays,
            int postReleaseDays,
            string? serviceFilter,
            CancellationToken ct = default);
    }

    // ── Null reader ────────────────────────────────────────────────────────

    /// <summary>Implementação nula — devolve lista vazia (para testes e bootstrap).</summary>
    public sealed class NullCostPerReleaseReader : ICostPerReleaseReader
    {
        public Task<IReadOnlyList<ReleaseCostData>> ListByTenantAsync(
            string tenantId, int lookbackDays, int preReleaseDays, int postReleaseDays,
            string? serviceFilter, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ReleaseCostData>>([]);
    }

    // ── Validator ──────────────────────────────────────────────────────────

    /// <summary>Valida os parâmetros do Query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LookbackDays).InclusiveBetween(7, 90);
            RuleFor(x => x.PreReleaseDays).InclusiveBetween(3, 30);
            RuleFor(x => x.PostReleaseDays).InclusiveBetween(3, 30);
            RuleFor(x => x.SpikeThresholdPct).InclusiveBetween(50.0, 500.0);
            RuleFor(x => x.TopReleasesCount).InclusiveBetween(1, 50);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    /// <summary>Handler da query GetCostPerReleaseReport.</summary>
    public sealed class Handler(
        ICostPerReleaseReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query query, CancellationToken ct)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var rawData = await reader.ListByTenantAsync(
                query.TenantId, query.LookbackDays, query.PreReleaseDays,
                query.PostReleaseDays, query.ServiceFilter, ct);

            var entries = rawData.Select(d => BuildEntry(d, query.SpikeThresholdPct)).ToList();

            var spikeEntries = entries.Where(e => e.ImpactTier == CostImpactTier.CostSpike).ToList();
            var savingEntries = entries.Where(e => e.ImpactTier == CostImpactTier.CostSaving).ToList();

            var avgCost = entries.Count > 0
                ? entries.Average(e => e.PostReleaseTotalCostUsd)
                : 0m;

            var totalWasted = entries.Where(e => e.WastedDeploymentCost).Sum(e => e.PostReleaseTotalCostUsd);

            var summary = new ReleaseCostSummary(
                TotalReleases: entries.Count,
                AvgCostPerRelease: Math.Round(avgCost, 2),
                CostSpikeCount: spikeEntries.Count,
                CostSavingCount: savingEntries.Count,
                CostSpikeRatePct: entries.Count > 0 ? Math.Round((double)spikeEntries.Count / entries.Count * 100.0, 1) : 0.0,
                CostSavingRatePct: entries.Count > 0 ? Math.Round((double)savingEntries.Count / entries.Count * 100.0, 1) : 0.0,
                TotalWastedDeploymentCostUsd: Math.Round(totalWasted, 2));

            return Result<Report>.Success(new Report(
                GeneratedAt: clock.UtcNow,
                LookbackDays: query.LookbackDays,
                TotalReleasesAnalyzed: entries.Count,
                Summary: summary,
                TopCostSpikeReleases: entries
                    .OrderByDescending(e => e.CostDeltaPct)
                    .Take(query.TopReleasesCount)
                    .ToList(),
                TopCostSavingReleases: entries
                    .OrderBy(e => e.CostDeltaPct)
                    .Take(query.TopReleasesCount)
                    .ToList(),
                AllReleases: entries.OrderByDescending(e => e.DeployedAt).ToList()));
        }

        private static ReleaseWithCostEntry BuildEntry(ReleaseCostData d, double spikeThreshold)
        {
            var delta = d.PreReleaseDailyAvgCostUsd > 0m
                ? (double)((d.PostReleaseDailyAvgCostUsd - d.PreReleaseDailyAvgCostUsd) / d.PreReleaseDailyAvgCostUsd * 100m)
                : (d.PostReleaseDailyAvgCostUsd > 0m ? double.MaxValue : 0.0);

            delta = Math.Round(delta, 2);

            // Apply custom spike threshold if provided
            var tier = ClassifyTier(delta, spikeThreshold);
            var wasted = d.IsFailedOrRolledBack && tier == CostImpactTier.CostSpike;

            return new ReleaseWithCostEntry(
                d.ReleaseId, d.ServiceName, d.Environment, d.DeployedAt,
                d.IsFailedOrRolledBack,
                Math.Round(d.PreReleaseDailyAvgCostUsd, 2),
                Math.Round(d.PostReleaseDailyAvgCostUsd, 2),
                Math.Round(d.PostReleaseTotalCostUsd, 2),
                delta, tier, wasted);
        }

        private static CostImpactTier ClassifyTier(double delta, double spikeThreshold) => delta switch
        {
            < CostSavingThreshold => CostImpactTier.CostSaving,
            <= NeutralUpperThreshold => CostImpactTier.Neutral,
            <= MinorIncreaseThreshold => CostImpactTier.MinorIncrease,
            var d when d <= spikeThreshold => CostImpactTier.MajorIncrease,
            _ => CostImpactTier.CostSpike
        };
    }
}
