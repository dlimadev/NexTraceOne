using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetFinOpsWasteAnalysisReport;

/// <summary>
/// Feature: GetFinOpsWasteAnalysisReport — análise consolidada de desperdício operacional por
/// serviço, cruzando múltiplos sinais de waste.
///
/// Para cada serviço, identifica quatro categorias de waste:
/// - <c>IdleWaste</c>: recursos idle com custo acima da mediana (LowLoad com custo elevado)
/// - <c>OverProvisioningWaste</c>: custo de não-prod desproporcional (ratio > threshold)
/// - <c>FailedDeploymentWaste</c>: custo acumulado de releases Failed ou RolledBack
/// - <c>DriftWaste</c>: custo de serviços com DriftFinding.Severity = High/Critical
///
/// <c>WasteScore</c> por serviço (0–100) — soma ponderada das categorias de waste:
/// - IdleWaste: 30 pontos
/// - OverProvisioningWaste: 25 pontos
/// - FailedDeploymentWaste: 25 pontos
/// - DriftWaste: 20 pontos
///
/// <c>WasteTier</c>:
/// - <c>Clean</c>       — WasteScore ≤ 10
/// - <c>Minor</c>       — WasteScore ≤ 30
/// - <c>Significant</c> — WasteScore ≤ 60
/// - <c>Critical</c>    — WasteScore > 60
///
/// Wave AG.3 — GetFinOpsWasteAnalysisReport (OperationalIntelligence FinOps).
/// </summary>
public static class GetFinOpsWasteAnalysisReport
{
    // ── WasteScore weights ──────────────────────────────────────────────────
    private const double IdleWasteWeight = 30.0;
    private const double OverProvisioningWeight = 25.0;
    private const double FailedDeploymentWeight = 25.0;
    private const double DriftWasteWeight = 20.0;

    // ── WasteTier thresholds ────────────────────────────────────────────────
    private const double CleanThreshold = 10.0;
    private const double MinorThreshold = 30.0;
    private const double SignificantThreshold = 60.0;

    private const int TopWasteServicesDefault = 10;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: período de análise em dias (7–90, default 30).</para>
    /// <para><c>MaxServices</c>: máximo de serviços no relatório (1–200, default 100).</para>
    /// <para><c>SignificantWasteThreshold</c>: threshold de WasteScore para WasteTier Significant (10–80, default 30).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        int MaxServices = 100,
        double SignificantWasteThreshold = MinorThreshold) : IQuery<Report>;

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>Categoria de desperdício operacional identificada para um serviço.</summary>
    public enum WasteCategory
    {
        /// <summary>Recurso idle com custo acima da mediana do tenant.</summary>
        IdleWaste,
        /// <summary>Custo em não-produção desproporcional ao custo em produção.</summary>
        OverProvisioningWaste,
        /// <summary>Custo acumulado de releases falhadas ou revertidas.</summary>
        FailedDeploymentWaste,
        /// <summary>Custo de serviços com drift de alta severidade não resolvido.</summary>
        DriftWaste
    }

    /// <summary>Classificação de nível de desperdício por WasteScore.</summary>
    public enum WasteTier
    {
        /// <summary>WasteScore ≤ 10 — sem sinais significativos de desperdício.</summary>
        Clean,
        /// <summary>WasteScore ≤ 30 — desperdício menor e controlável.</summary>
        Minor,
        /// <summary>WasteScore ≤ 60 — desperdício significativo que requer atenção.</summary>
        Significant,
        /// <summary>WasteScore > 60 — desperdício crítico com impacto alto no orçamento.</summary>
        Critical
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Distribuição percentual de waste por categoria.</summary>
    public sealed record WasteCategoryDistribution(
        double IdleWastePct,
        double OverProvisioningPct,
        double FailedDeploymentPct,
        double DriftWastePct);

    /// <summary>Oportunidade de poupança para um serviço específico.</summary>
    public sealed record WasteOpportunity(
        string ServiceName,
        decimal EstimatedSavingsUsd,
        WasteTier Tier,
        IReadOnlyList<WasteCategory> Categories);

    /// <summary>Entrada de análise de waste por serviço.</summary>
    public sealed record ServiceWasteEntry(
        string ServiceName,
        string? TeamId,
        double WasteScore,
        WasteTier Tier,
        decimal EstimatedWasteUsd,
        bool HasIdleWaste,
        bool HasOverProvisioningWaste,
        bool HasFailedDeploymentWaste,
        bool HasDriftWaste,
        IReadOnlyList<WasteCategory> ActiveCategories);

    /// <summary>Relatório consolidado de análise de desperdício operacional.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        int TotalServicesAnalyzed,
        decimal TotalEstimatedWasteUsd,
        decimal WasteOpportunitySavingsUsd,
        WasteCategoryDistribution WasteByCategory,
        IReadOnlyList<ServiceWasteEntry> TopWasteServices,
        IReadOnlyList<WasteOpportunity> WasteOpportunities,
        IReadOnlyList<ServiceWasteEntry> AllServices);

    // ── Reader abstraction ─────────────────────────────────────────────────

    /// <summary>Dados de sinais de waste para um serviço.</summary>
    public sealed record ServiceWasteSignals(
        string ServiceName,
        string? TeamId,
        bool IsIdleResource,
        decimal ServiceCostUsd,
        decimal MedianTenantCostUsd,
        bool HasExcessiveNonProdCost,
        decimal NonProdWasteCostUsd,
        bool HasFailedDeployments,
        decimal FailedDeploymentCostUsd,
        bool HasHighSeverityDrift,
        decimal DriftEstimatedCostImpactUsd);

    /// <summary>Reader de sinais de waste consolidados para o relatório AG.3.</summary>
    public interface IFinOpsWasteReader
    {
        /// <summary>Devolve sinais de waste por serviço no período.</summary>
        Task<IReadOnlyList<ServiceWasteSignals>> ListByTenantAsync(
            string tenantId,
            int lookbackDays,
            int maxServices,
            CancellationToken ct = default);
    }

    // ── Null reader ────────────────────────────────────────────────────────

    /// <summary>Implementação nula — devolve lista vazia (para testes e bootstrap).</summary>
    public sealed class NullFinOpsWasteReader : IFinOpsWasteReader
    {
        public Task<IReadOnlyList<ServiceWasteSignals>> ListByTenantAsync(
            string tenantId, int lookbackDays, int maxServices, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ServiceWasteSignals>>([]);
    }

    // ── Validator ──────────────────────────────────────────────────────────

    /// <summary>Valida os parâmetros do Query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LookbackDays).InclusiveBetween(7, 90);
            RuleFor(x => x.MaxServices).InclusiveBetween(1, 200);
            RuleFor(x => x.SignificantWasteThreshold).InclusiveBetween(10.0, 80.0);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    /// <summary>Handler da query GetFinOpsWasteAnalysisReport.</summary>
    public sealed class Handler(
        IFinOpsWasteReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query query, CancellationToken ct)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var signals = await reader.ListByTenantAsync(
                query.TenantId, query.LookbackDays, query.MaxServices, ct);

            var entries = signals.Select(s => BuildEntry(s, query.SignificantWasteThreshold)).ToList();

            var topWaste = entries
                .OrderByDescending(e => e.WasteScore)
                .Take(TopWasteServicesDefault)
                .ToList();

            // WasteByCategory — share of total waste cost per category
            var totalIdleWaste = entries.Where(e => e.HasIdleWaste).Sum(e => e.EstimatedWasteUsd);
            var totalOverProv = entries.Where(e => e.HasOverProvisioningWaste).Sum(e => e.EstimatedWasteUsd);
            var totalFailed = entries.Where(e => e.HasFailedDeploymentWaste).Sum(e => e.EstimatedWasteUsd);
            var totalDrift = entries.Where(e => e.HasDriftWaste).Sum(e => e.EstimatedWasteUsd);
            var grandTotal = totalIdleWaste + totalOverProv + totalFailed + totalDrift;

            var distribution = grandTotal > 0m
                ? new WasteCategoryDistribution(
                    Math.Round((double)(totalIdleWaste / grandTotal * 100m), 1),
                    Math.Round((double)(totalOverProv / grandTotal * 100m), 1),
                    Math.Round((double)(totalFailed / grandTotal * 100m), 1),
                    Math.Round((double)(totalDrift / grandTotal * 100m), 1))
                : new WasteCategoryDistribution(0, 0, 0, 0);

            var opportunities = topWaste.Select(e => new WasteOpportunity(
                e.ServiceName,
                e.EstimatedWasteUsd,
                e.Tier,
                e.ActiveCategories)).ToList();

            var opSavings = opportunities.Sum(o => o.EstimatedSavingsUsd);

            return Result<Report>.Success(new Report(
                GeneratedAt: clock.UtcNow,
                LookbackDays: query.LookbackDays,
                TotalServicesAnalyzed: entries.Count,
                TotalEstimatedWasteUsd: entries.Sum(e => e.EstimatedWasteUsd),
                WasteOpportunitySavingsUsd: opSavings,
                WasteByCategory: distribution,
                TopWasteServices: topWaste,
                WasteOpportunities: opportunities,
                AllServices: entries.OrderByDescending(e => e.WasteScore).ToList()));
        }

        private static ServiceWasteEntry BuildEntry(ServiceWasteSignals s, double significantThreshold)
        {
            // Dimension score (0–100 per category, weighted)
            var idleScore = s.IsIdleResource && s.ServiceCostUsd > s.MedianTenantCostUsd
                ? IdleWasteWeight
                : 0.0;
            var overProvScore = s.HasExcessiveNonProdCost ? OverProvisioningWeight : 0.0;
            var failedScore = s.HasFailedDeployments ? FailedDeploymentWeight : 0.0;
            var driftScore = s.HasHighSeverityDrift ? DriftWasteWeight : 0.0;

            var wasteScore = Math.Min(100.0, idleScore + overProvScore + failedScore + driftScore);

            // Use custom significantThreshold to shift Minor/Significant boundary
            var tier = ClassifyTier(wasteScore, significantThreshold);

            var estimatedWaste = s.NonProdWasteCostUsd + s.FailedDeploymentCostUsd + s.DriftEstimatedCostImpactUsd
                + (s.IsIdleResource && s.ServiceCostUsd > s.MedianTenantCostUsd
                    ? s.ServiceCostUsd - s.MedianTenantCostUsd
                    : 0m);

            var categories = new List<WasteCategory>();
            if (idleScore > 0) categories.Add(WasteCategory.IdleWaste);
            if (overProvScore > 0) categories.Add(WasteCategory.OverProvisioningWaste);
            if (failedScore > 0) categories.Add(WasteCategory.FailedDeploymentWaste);
            if (driftScore > 0) categories.Add(WasteCategory.DriftWaste);

            return new ServiceWasteEntry(
                ServiceName: s.ServiceName,
                TeamId: s.TeamId,
                WasteScore: Math.Round(wasteScore, 1),
                Tier: tier,
                EstimatedWasteUsd: Math.Round(estimatedWaste, 2),
                HasIdleWaste: idleScore > 0,
                HasOverProvisioningWaste: overProvScore > 0,
                HasFailedDeploymentWaste: failedScore > 0,
                HasDriftWaste: driftScore > 0,
                ActiveCategories: categories);
        }

        private static WasteTier ClassifyTier(double score, double significantThreshold) => score switch
        {
            <= CleanThreshold => WasteTier.Clean,
            var s when s <= significantThreshold => WasteTier.Minor,
            <= SignificantThreshold => WasteTier.Significant,
            _ => WasteTier.Critical
        };
    }
}
