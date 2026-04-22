using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetCrossTenantMaturityReport;

/// <summary>
/// Feature: GetCrossTenantMaturityReport — comparação anónima e consentida de maturidade entre tenants.
///
/// Para cada tenant com <c>TenantBenchmarkConsent.ConsentGiven = true</c>, calcula
/// <c>TenantMaturityScore</c> (0–100) em 7 dimensões ponderadas igualmente:
/// - ContractGoverned            — % serviços com contratos aprovados
/// - ChangeConfidenceEnabled     — % releases com ConfidenceScore registado
/// - SloTracked                  — % serviços com SloObservation no último mês
/// - RunbookCovered              — % incidentes com runbook associado
/// - ProfilingActive             — % serviços com ProfilingSession no último mês
/// - ComplianceEvaluated         — % serviços avaliados em ≥ 1 standard no trimestre
/// - AiAssistantUsed             — % utilizadores activos com interação AI assistant
///
/// Classifica por <c>MaturityTier</c>:
/// - <c>Pioneer</c>   — score ≥ 85
/// - <c>Advanced</c>  — score ≥ 65
/// - <c>Developing</c>— score ≥ 40
/// - <c>Emerging</c>  — score &lt; 40
///
/// Privacidade: o benchmark usa apenas mediana e percentis anónimos — nunca dados de tenant individual.
/// Requer pelo menos <c>min_tenants_for_benchmark</c> participantes consentidos para exibir percentil.
///
/// Endpoint: <c>GET /api/v1/governance/maturity/cross-tenant-benchmark</c>
///
/// Wave AJ.1 — Multi-Tenant Governance Intelligence (ChangeGovernance Compliance).
/// </summary>
public static class GetCrossTenantMaturityReport
{
    // ── Tier thresholds ────────────────────────────────────────────────────
    private const decimal PioneerThreshold = 85m;
    private const decimal AdvancedThreshold = 65m;
    private const decimal DevelopingThreshold = 40m;

    private const int WeakestDimensionCount = 3;
    internal const int DefaultMinTenantsForBenchmark = 5;
    internal const int DefaultLookbackMonths = 1;

    // ── Query ──────────────────────────────────────────────────────────────

    /// <summary>
    /// <para><c>TenantId</c>: tenant a analisar (obrigatório).</para>
    /// <para><c>LookbackMonths</c>: período de análise em meses (1–12, default 1).</para>
    /// <para><c>MinTenantsForBenchmark</c>: mínimo de tenants consentidos para exibir percentil (2–100, default 5).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackMonths = DefaultLookbackMonths,
        int MinTenantsForBenchmark = DefaultMinTenantsForBenchmark) : IQuery<Report>;

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>Tier de maturidade do tenant na plataforma.</summary>
    public enum MaturityTier
    {
        /// <summary>Score &lt; 40 — adopção inicial, grande margem de melhoria.</summary>
        Emerging,
        /// <summary>Score ≥ 40 — adopção em desenvolvimento, processo a ganhar forma.</summary>
        Developing,
        /// <summary>Score ≥ 65 — adopção avançada, a maioria das capacidades em uso.</summary>
        Advanced,
        /// <summary>Score ≥ 85 — adopção exemplar, referência no ecossistema.</summary>
        Pioneer
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Score de uma dimensão de maturidade e gap vs. benchmark mediano.</summary>
    public sealed record MaturityDimensionScore(
        string DimensionName,
        decimal Score,
        decimal MedianBenchmark,
        decimal GapVsMedian);

    /// <summary>Benchmark anónimo do ecossistema por dimensão.</summary>
    public sealed record EcosystemBenchmark(
        int ParticipatingTenants,
        IReadOnlyDictionary<string, decimal> MedianByDimension,
        IReadOnlyDictionary<string, decimal> P25ByDimension,
        IReadOnlyDictionary<string, decimal> P75ByDimension);

    /// <summary>Resultado do relatório de maturidade cross-tenant.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string TenantId,
        int LookbackMonths,
        decimal TenantMaturityScore,
        MaturityTier Tier,
        IReadOnlyList<MaturityDimensionScore> Dimensions,
        decimal? BenchmarkPercentile,
        bool InsufficientBenchmarkPeers,
        IReadOnlyList<string> WeakestDimensions,
        decimal ImprovementPotential,
        EcosystemBenchmark? Benchmark);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackMonths).InclusiveBetween(1, 12);
            RuleFor(q => q.MinTenantsForBenchmark).InclusiveBetween(2, 100);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly ICrossTenantMaturityReader _reader;
        private readonly IDateTimeProvider _clock;

        public Handler(
            ICrossTenantMaturityReader reader,
            IDateTimeProvider clock)
        {
            _reader = Guard.Against.Null(reader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var since = now.AddMonths(-query.LookbackMonths);

            // 1. Get dimensions for the requesting tenant
            var tenantDims = await _reader.GetDimensionsAsync(query.TenantId, since, cancellationToken);
            var scores = ExtractDimensionScores(tenantDims);

            decimal maturityScore = Math.Round(scores.Values.Average(), 1);
            maturityScore = Math.Clamp(maturityScore, 0m, 100m);
            var tier = ClassifyTier(maturityScore);

            // 2. Get ecosystem benchmark (only consented tenants, anonymized)
            var ecosystemDims = await _reader.ListConsentedTenantDimensionsAsync(since, cancellationToken);
            var peers = ecosystemDims
                .Where(d => !string.Equals(d.TenantId, query.TenantId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            bool insufficientPeers = peers.Count < query.MinTenantsForBenchmark;

            EcosystemBenchmark? benchmark = null;
            decimal? benchmarkPercentile = null;

            if (!insufficientPeers)
            {
                var (medians, p25s, p75s) = ComputePercentileStats(peers);
                benchmark = new EcosystemBenchmark(peers.Count, medians, p25s, p75s);

                var peerScores = peers
                    .Select(p => ExtractDimensionScores(p).Values.Average())
                    .OrderBy(s => s)
                    .ToList();

                benchmarkPercentile = ComputePercentile(maturityScore, peerScores);
            }

            // 3. Build dimension breakdown with gaps vs. median benchmark
            var dimensionBreakdown = BuildDimensionBreakdown(scores, benchmark);

            // 4. Identify weakest dimensions (highest positive gap vs median)
            var weakest = dimensionBreakdown
                .Where(d => d.GapVsMedian > 0)
                .OrderByDescending(d => d.GapVsMedian)
                .Take(WeakestDimensionCount)
                .Select(d => d.DimensionName)
                .ToList();

            // 5. Improvement potential: gain if weakest dimensions reach median
            decimal improvementPotential = CalculateImprovementPotential(scores, benchmark, weakest);

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                TenantId: query.TenantId,
                LookbackMonths: query.LookbackMonths,
                TenantMaturityScore: maturityScore,
                Tier: tier,
                Dimensions: dimensionBreakdown,
                BenchmarkPercentile: benchmarkPercentile,
                InsufficientBenchmarkPeers: insufficientPeers,
                WeakestDimensions: weakest,
                ImprovementPotential: Math.Round(improvementPotential, 1),
                Benchmark: benchmark));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static Dictionary<string, decimal> ExtractDimensionScores(
            ICrossTenantMaturityReader.TenantMaturityDimensions d) => new()
        {
            ["ContractGoverned"] = Math.Clamp(d.ContractGoverned, 0m, 100m),
            ["ChangeConfidenceEnabled"] = Math.Clamp(d.ChangeConfidenceEnabled, 0m, 100m),
            ["SloTracked"] = Math.Clamp(d.SloTracked, 0m, 100m),
            ["RunbookCovered"] = Math.Clamp(d.RunbookCovered, 0m, 100m),
            ["ProfilingActive"] = Math.Clamp(d.ProfilingActive, 0m, 100m),
            ["ComplianceEvaluated"] = Math.Clamp(d.ComplianceEvaluated, 0m, 100m),
            ["AiAssistantUsed"] = Math.Clamp(d.AiAssistantUsed, 0m, 100m)
        };

        private static (Dictionary<string, decimal> medians,
                        Dictionary<string, decimal> p25s,
                        Dictionary<string, decimal> p75s)
            ComputePercentileStats(IReadOnlyList<ICrossTenantMaturityReader.TenantMaturityDimensions> peers)
        {
            var dimNames = new[]
            {
                "ContractGoverned", "ChangeConfidenceEnabled", "SloTracked",
                "RunbookCovered", "ProfilingActive", "ComplianceEvaluated", "AiAssistantUsed"
            };

            var medians = new Dictionary<string, decimal>();
            var p25s = new Dictionary<string, decimal>();
            var p75s = new Dictionary<string, decimal>();

            foreach (var dim in dimNames)
            {
                var sorted = peers
                    .Select(p => ExtractDimensionScores(p).GetValueOrDefault(dim, 0m))
                    .OrderBy(v => v)
                    .ToList();

                medians[dim] = PercentileOf(sorted, 50);
                p25s[dim] = PercentileOf(sorted, 25);
                p75s[dim] = PercentileOf(sorted, 75);
            }

            return (medians, p25s, p75s);
        }

        private static IReadOnlyList<MaturityDimensionScore> BuildDimensionBreakdown(
            Dictionary<string, decimal> scores,
            EcosystemBenchmark? benchmark)
        {
            return scores.Select(kv =>
            {
                decimal median = benchmark?.MedianByDimension.GetValueOrDefault(kv.Key, 0m) ?? 0m;
                decimal gap = Math.Round(Math.Max(0m, median - kv.Value), 1);
                return new MaturityDimensionScore(kv.Key, Math.Round(kv.Value, 1), Math.Round(median, 1), gap);
            }).ToList();
        }

        private static decimal CalculateImprovementPotential(
            Dictionary<string, decimal> scores,
            EcosystemBenchmark? benchmark,
            IReadOnlyList<string> weakestDims)
        {
            if (benchmark is null || weakestDims.Count == 0 || scores.Count == 0) return 0m;

            decimal baseScore = scores.Values.Average();
            var improved = new Dictionary<string, decimal>(scores);

            foreach (var dim in weakestDims)
            {
                if (benchmark.MedianByDimension.TryGetValue(dim, out decimal median))
                    improved[dim] = Math.Max(scores.GetValueOrDefault(dim, 0m), median);
            }

            decimal improvedScore = improved.Values.Average();
            return Math.Max(0m, improvedScore - baseScore);
        }

        private static decimal ComputePercentile(decimal value, List<decimal> sorted)
        {
            if (sorted.Count == 0) return 50m;
            int below = sorted.Count(v => v < value);
            return Math.Round((decimal)below / sorted.Count * 100m, 0);
        }

        internal static MaturityTier ClassifyTier(decimal score) => score switch
        {
            >= PioneerThreshold => MaturityTier.Pioneer,
            >= AdvancedThreshold => MaturityTier.Advanced,
            >= DevelopingThreshold => MaturityTier.Developing,
            _ => MaturityTier.Emerging
        };

        private static decimal PercentileOf(List<decimal> sorted, int pct)
        {
            if (sorted.Count == 0) return 0m;
            int idx = (int)Math.Ceiling(pct / 100.0 * sorted.Count) - 1;
            idx = Math.Clamp(idx, 0, sorted.Count - 1);
            return sorted[idx];
        }
    }
}
