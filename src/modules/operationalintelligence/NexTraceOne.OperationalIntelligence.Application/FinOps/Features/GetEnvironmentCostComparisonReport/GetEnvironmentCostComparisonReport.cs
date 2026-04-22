using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetEnvironmentCostComparisonReport;

/// <summary>
/// Feature: GetEnvironmentCostComparisonReport — comparação de custo operacional entre ambientes
/// (dev/staging/prod) por serviço.
///
/// Identifica serviços onde o custo em não-produção excede de forma desproporcional o custo em
/// produção — sinal de desperdício em ambientes de teste.
///
/// Para cada serviço com custo registado em múltiplos ambientes, calcula:
/// - <c>ProdCostUsd</c>: custo total em ambiente Production
/// - <c>NonProdCostUsd</c>: custo total em ambientes não-produção (soma)
/// - <c>NonProdToProdRatio</c>: NonProdCostUsd / ProdCostUsd
/// - <c>NonProdWasteCostUsd</c>: custo excedente não-prod vs. prod (se ratio > ExpectedRatio)
///
/// <c>EnvironmentEfficiencyTier</c>:
/// - <c>Optimal</c>       — ratio ≤ 0.5 (não-prod custa metade ou menos — esperado)
/// - <c>Acceptable</c>    — ratio ≤ 1.0
/// - <c>Overprovisioned</c> — ratio ≤ 2.0 (não-prod custa mais que prod)
/// - <c>WasteAlert</c>    — ratio > 2.0 (non-prod custa 2× ou mais que prod)
///
/// Wave AG.1 — GetEnvironmentCostComparisonReport (OperationalIntelligence FinOps).
/// </summary>
public static class GetEnvironmentCostComparisonReport
{
    // ── Tier thresholds (ratio = NonProd / Prod) ───────────────────────────
    private const double OptimalThreshold = 0.5;
    private const double AcceptableThreshold = 1.0;
    private const double OverprovisionedThreshold = 2.0;

    private const string ProductionLabel = "production";

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: período de análise em dias (7–90, default 30).</para>
    /// <para><c>ExpectedNonProdRatio</c>: ratio non-prod/prod esperado para Optimal tier (0.1–2.0, default 0.5).</para>
    /// <para><c>TopServicesCount</c>: máximo de serviços com maior WasteCostUsd a listar (1–50, default 10).</para>
    /// <para><c>TeamFilter</c>: filtro opcional por equipa.</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        double ExpectedNonProdRatio = OptimalThreshold,
        int TopServicesCount = 10,
        string? TeamFilter = null) : IQuery<Report>;

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>Classificação de eficiência de custo entre ambientes por serviço.</summary>
    public enum EnvironmentEfficiencyTier
    {
        /// <summary>Ratio ≤ 0.5 — não-prod custa metade ou menos que prod (esperado).</summary>
        Optimal,
        /// <summary>Ratio ≤ 1.0 — custo similar entre ambientes.</summary>
        Acceptable,
        /// <summary>Ratio ≤ 2.0 — não-prod custa mais que prod (possível sobreprovisionamento).</summary>
        Overprovisioned,
        /// <summary>Ratio > 2.0 — não-prod custa o dobro ou mais que prod (sinal claro de desperdício).</summary>
        WasteAlert
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Distribuição de serviços por EnvironmentEfficiencyTier.</summary>
    public sealed record TierDistribution(
        int OptimalCount,
        int AcceptableCount,
        int OverprovisionedCount,
        int WasteAlertCount);

    /// <summary>Comparação de custo por ambiente para um serviço.</summary>
    public sealed record ServiceEnvironmentCostEntry(
        string ServiceName,
        string? TeamId,
        decimal ProdCostUsd,
        decimal NonProdCostUsd,
        double NonProdToProdRatio,
        decimal NonProdWasteCostUsd,
        EnvironmentEfficiencyTier Tier);

    /// <summary>Relatório de comparação de custo entre ambientes.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        int TotalServicesAnalyzed,
        decimal TotalNonProdWasteUsd,
        decimal TotalProdCostUsd,
        decimal TotalNonProdCostUsd,
        TierDistribution DistributionByTier,
        IReadOnlyList<ServiceEnvironmentCostEntry> TopWasteServices,
        IReadOnlyList<ServiceEnvironmentCostEntry> AllServices);

    // ── Reader abstraction ─────────────────────────────────────────────────

    /// <summary>Registo de custo por serviço e ambiente para comparação.</summary>
    public sealed record EnvironmentCostRecord(
        string ServiceName,
        string? TeamId,
        string Environment,
        decimal TotalCostUsd);

    /// <summary>Reader de dados de custo por ambiente para o relatório AG.1.</summary>
    public interface IEnvironmentCostComparisonReader
    {
        /// <summary>Devolve registos de custo por serviço e ambiente no período.</summary>
        Task<IReadOnlyList<EnvironmentCostRecord>> ListByTenantAsync(
            string tenantId,
            int lookbackDays,
            string? teamFilter,
            CancellationToken ct = default);
    }

    // ── Null reader ────────────────────────────────────────────────────────

    /// <summary>Implementação nula — devolve lista vazia (para testes e bootstrap).</summary>
    public sealed class NullEnvironmentCostComparisonReader : IEnvironmentCostComparisonReader
    {
        public Task<IReadOnlyList<EnvironmentCostRecord>> ListByTenantAsync(
            string tenantId, int lookbackDays, string? teamFilter, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<EnvironmentCostRecord>>([]);
    }

    // ── Validator ──────────────────────────────────────────────────────────

    /// <summary>Valida os parâmetros do Query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LookbackDays).InclusiveBetween(7, 90);
            RuleFor(x => x.ExpectedNonProdRatio).InclusiveBetween(0.1, 2.0);
            RuleFor(x => x.TopServicesCount).InclusiveBetween(1, 50);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    /// <summary>Handler da query GetEnvironmentCostComparisonReport.</summary>
    public sealed class Handler(
        IEnvironmentCostComparisonReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query query, CancellationToken ct)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var records = await reader.ListByTenantAsync(
                query.TenantId, query.LookbackDays, query.TeamFilter, ct);

            // Group by service name
            var byService = records
                .GroupBy(r => r.ServiceName)
                .ToList();

            var entries = new List<ServiceEnvironmentCostEntry>();

            foreach (var group in byService)
            {
                var serviceName = group.Key;
                var teamId = group.FirstOrDefault(r => r.TeamId is not null)?.TeamId;

                var prodCost = group
                    .Where(r => r.Environment.Equals(ProductionLabel, StringComparison.OrdinalIgnoreCase))
                    .Sum(r => r.TotalCostUsd);

                var nonProdCost = group
                    .Where(r => !r.Environment.Equals(ProductionLabel, StringComparison.OrdinalIgnoreCase))
                    .Sum(r => r.TotalCostUsd);

                // Only include services with prod cost (otherwise ratio is meaningless)
                if (prodCost <= 0m)
                    continue;

                var ratio = (double)(nonProdCost / prodCost);
                var tier = ClassifyTier(ratio);
                var waste = CalculateWaste(nonProdCost, prodCost, (decimal)query.ExpectedNonProdRatio);

                entries.Add(new ServiceEnvironmentCostEntry(
                    serviceName, teamId, prodCost, nonProdCost,
                    Math.Round(ratio, 3), waste, tier));
            }

            var topWaste = entries
                .OrderByDescending(e => e.NonProdWasteCostUsd)
                .Take(query.TopServicesCount)
                .ToList();

            var distribution = new TierDistribution(
                entries.Count(e => e.Tier == EnvironmentEfficiencyTier.Optimal),
                entries.Count(e => e.Tier == EnvironmentEfficiencyTier.Acceptable),
                entries.Count(e => e.Tier == EnvironmentEfficiencyTier.Overprovisioned),
                entries.Count(e => e.Tier == EnvironmentEfficiencyTier.WasteAlert));

            return Result<Report>.Success(new Report(
                GeneratedAt: clock.UtcNow,
                LookbackDays: query.LookbackDays,
                TotalServicesAnalyzed: entries.Count,
                TotalNonProdWasteUsd: entries.Sum(e => e.NonProdWasteCostUsd),
                TotalProdCostUsd: entries.Sum(e => e.ProdCostUsd),
                TotalNonProdCostUsd: entries.Sum(e => e.NonProdCostUsd),
                DistributionByTier: distribution,
                TopWasteServices: topWaste,
                AllServices: entries.OrderByDescending(e => e.NonProdWasteCostUsd).ToList()));
        }

        private static EnvironmentEfficiencyTier ClassifyTier(double ratio) => ratio switch
        {
            <= OptimalThreshold => EnvironmentEfficiencyTier.Optimal,
            <= AcceptableThreshold => EnvironmentEfficiencyTier.Acceptable,
            <= OverprovisionedThreshold => EnvironmentEfficiencyTier.Overprovisioned,
            _ => EnvironmentEfficiencyTier.WasteAlert
        };

        private static decimal CalculateWaste(decimal nonProdCost, decimal prodCost, decimal expectedRatio)
        {
            var expected = prodCost * expectedRatio;
            var excess = nonProdCost - expected;
            return excess > 0m ? Math.Round(excess, 2) : 0m;
        }
    }
}
