using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetApiBackwardCompatibilityReport;

/// <summary>
/// Feature: GetApiBackwardCompatibilityReport — scorecard de compatibilidade retroativa por contrato.
///
/// Mede quão "safe to evolve" é cada contrato para os seus consumidores, com perspectiva longitudinal:
/// - <c>BreakingChangeRate</c>: % de changelogs com breaking change no período
/// - <c>MajorVersionCount</c>: versões major lançadas no período
/// - <c>ConsumerAdoptionLagDays</c>: dias médios para consumidores migrarem
/// - <c>BackwardCompatibilityScore</c>: <c>(1 - BreakingChangeRate) * 100</c> ajustado por <c>ConsumerAdoptionLagDays</c>
///
/// <c>CompatibilityTier</c>:
/// - <c>Stable</c>    — Score ≥ <c>StableThreshold</c> (default 85) e BreakingChangeRate &lt; 10%
/// - <c>Evolving</c>  — Score ≥ 65, breaking changes controladas
/// - <c>Volatile</c>  — Score ≥ 40, breaking changes frequentes ou adopção lenta
/// - <c>Unstable</c>  — Score &lt; 40
///
/// <c>StagnationFlag</c>: contratos Stable sem changelogs há mais de <c>StagnationDays</c> dias.
///
/// Complementa o snapshot de saúde actual (Waves M.1/O.1/R.2) com perspectiva longitudinal
/// de qualidade de evolução do catálogo de contratos.
///
/// Wave AE.3 — GetApiBackwardCompatibilityReport (Catalog Contracts).
/// </summary>
public static class GetApiBackwardCompatibilityReport
{
    // ── Score thresholds ──────────────────────────────────────────────────
    private const double DefaultStableThreshold = 85.0;
    private const double EvolvingThreshold = 65.0;
    private const double VolatileThreshold = 40.0;

    // ── BreakingChangeRate limit for Stable tier ──────────────────────────
    private const double StableMaxBreakingRatePct = 10.0;

    // ── Penalidade por lag (por dia acima de 7 dias) ──────────────────────
    private const double AdoptionLagPenaltyPerDay = 0.1;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal de análise em dias (30–365, default 180).</para>
    /// <para><c>StableThreshold</c>: score mínimo para CompatibilityTier Stable (50–100, default 85).</para>
    /// <para><c>StagnationDays</c>: dias sem changelog para activar StagnationFlag (30–730, default 180).</para>
    /// <para><c>MaxContracts</c>: máximo de contratos no relatório (10–500, default 200).</para>
    /// <para><c>TopVolatileCount</c>: número máximo de contratos mais voláteis a listar (1–50, default 10).</para>
    /// <para><c>TopStableCount</c>: número máximo de contratos mais estáveis a listar (1–50, default 10).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 180,
        double StableThreshold = DefaultStableThreshold,
        int StagnationDays = 180,
        int MaxContracts = 200,
        int TopVolatileCount = 10,
        int TopStableCount = 10) : IQuery<Report>;

    // ── Enums ─────────────────────────────────────────────────────────────

    /// <summary>Classificação de compatibilidade retroativa de um contrato.</summary>
    public enum CompatibilityTier
    {
        /// <summary>Score ≥ StableThreshold e BreakingChangeRate &lt; 10%. Evolução controlada.</summary>
        Stable,
        /// <summary>Score ≥ 65. Breaking changes existem mas com adopção rápida.</summary>
        Evolving,
        /// <summary>Score ≥ 40. Breaking changes frequentes ou adopção lenta pelos consumidores.</summary>
        Volatile,
        /// <summary>Score &lt; 40. Breaking changes frequentes e consumidores sem migrar.</summary>
        Unstable
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Distribuição de contratos por tier de compatibilidade.</summary>
    public sealed record CompatibilityTierDistribution(
        int StableCount,
        int EvolvingCount,
        int VolatileCount,
        int UnstableCount);

    /// <summary>Scorecard de compatibilidade retroativa de um contrato individual.</summary>
    public sealed record ContractCompatibilityProfile(
        string ApiAssetId,
        string ServiceName,
        string LatestVersion,
        int TotalChangelogs,
        int BreakingChangelogCount,
        double BreakingChangeRatePct,
        int MajorVersionCount,
        double ConsumerAdoptionLagDays,
        double BackwardCompatibilityScore,
        CompatibilityTier CompatibilityTier,
        bool StagnationFlag,
        DateTimeOffset LastChangelogAt);

    /// <summary>Resultado do relatório de compatibilidade retroativa de contratos.</summary>
    public sealed record Report(
        string TenantId,
        int TotalContractsAnalyzed,
        double TenantCompatibilityIndex,
        CompatibilityTierDistribution TierDistribution,
        int StagnationFlagCount,
        IReadOnlyList<ContractCompatibilityProfile> TopStableContracts,
        IReadOnlyList<ContractCompatibilityProfile> TopVolatileContracts,
        IReadOnlyList<ContractCompatibilityProfile> AllContracts);

    // ── Handler ───────────────────────────────────────────────────────────

    internal sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IContractCompatibilityReader _compatibilityReader;
        private readonly IDateTimeProvider _clock;

        public Handler(IContractCompatibilityReader compatibilityReader, IDateTimeProvider clock)
        {
            _compatibilityReader = Guard.Against.Null(compatibilityReader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken ct)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var entries = await _compatibilityReader.ListByTenantAsync(
                query.TenantId, query.LookbackDays, ct);

            var now = _clock.UtcNow;
            var profiles = new List<ContractCompatibilityProfile>();

            foreach (var entry in entries.Take(query.MaxContracts))
            {
                var breakingRatePct = entry.TotalChangelogs > 0
                    ? (double)entry.BreakingChangelogCount / entry.TotalChangelogs * 100.0
                    : 0.0;

                // Score base = (1 - BreakingChangeRate) * 100
                var baseScore = (1.0 - breakingRatePct / 100.0) * 100.0;

                // Penalidade por lag de adopção (por dia acima de 7 dias base)
                var lagPenalty = entry.ConsumerAdoptionLagDays > 7
                    ? (entry.ConsumerAdoptionLagDays - 7) * AdoptionLagPenaltyPerDay
                    : 0.0;

                var score = Math.Max(0.0, Math.Round(baseScore - lagPenalty, 1));

                var tier = ClassifyCompatibility(score, breakingRatePct, query.StableThreshold);

                // StagnationFlag: contratos Stable sem changelogs há mais de StagnationDays
                var daysSinceLastChange = (now - entry.LastChangelogAt).TotalDays;
                var stagnationFlag = tier == CompatibilityTier.Stable
                    && daysSinceLastChange > query.StagnationDays;

                profiles.Add(new ContractCompatibilityProfile(
                    ApiAssetId: entry.ApiAssetId,
                    ServiceName: entry.ServiceName,
                    LatestVersion: entry.LatestVersion,
                    TotalChangelogs: entry.TotalChangelogs,
                    BreakingChangelogCount: entry.BreakingChangelogCount,
                    BreakingChangeRatePct: Math.Round(breakingRatePct, 1),
                    MajorVersionCount: entry.MajorVersionCount,
                    ConsumerAdoptionLagDays: Math.Round(entry.ConsumerAdoptionLagDays, 1),
                    BackwardCompatibilityScore: score,
                    CompatibilityTier: tier,
                    StagnationFlag: stagnationFlag,
                    LastChangelogAt: entry.LastChangelogAt));
            }

            // TenantCompatibilityIndex = média ponderada de scores
            var tenantIndex = profiles.Count > 0
                ? Math.Round(profiles.Average(p => p.BackwardCompatibilityScore), 1)
                : 0.0;

            var tierDist = new CompatibilityTierDistribution(
                StableCount: profiles.Count(p => p.CompatibilityTier == CompatibilityTier.Stable),
                EvolvingCount: profiles.Count(p => p.CompatibilityTier == CompatibilityTier.Evolving),
                VolatileCount: profiles.Count(p => p.CompatibilityTier == CompatibilityTier.Volatile),
                UnstableCount: profiles.Count(p => p.CompatibilityTier == CompatibilityTier.Unstable));

            var topStable = profiles
                .Where(p => !p.StagnationFlag)
                .OrderByDescending(p => p.BackwardCompatibilityScore)
                .Take(query.TopStableCount)
                .ToList();

            var topVolatile = profiles
                .OrderBy(p => p.BackwardCompatibilityScore)
                .Take(query.TopVolatileCount)
                .ToList();

            return Result<Report>.Success(new Report(
                TenantId: query.TenantId,
                TotalContractsAnalyzed: profiles.Count,
                TenantCompatibilityIndex: tenantIndex,
                TierDistribution: tierDist,
                StagnationFlagCount: profiles.Count(p => p.StagnationFlag),
                TopStableContracts: topStable,
                TopVolatileContracts: topVolatile,
                AllContracts: profiles.OrderByDescending(p => p.BackwardCompatibilityScore).ToList()));
        }

        private static CompatibilityTier ClassifyCompatibility(
            double score,
            double breakingRatePct,
            double stableThreshold)
        {
            if (score >= stableThreshold && breakingRatePct < StableMaxBreakingRatePct)
                return CompatibilityTier.Stable;
            if (score >= EvolvingThreshold)
                return CompatibilityTier.Evolving;
            if (score >= VolatileThreshold)
                return CompatibilityTier.Volatile;
            return CompatibilityTier.Unstable;
        }
    }

    // ── Validator ─────────────────────────────────────────────────────────

    internal sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(q => q.LookbackDays).InclusiveBetween(30, 365);
            RuleFor(q => q.StableThreshold).InclusiveBetween(50.0, 100.0);
            RuleFor(q => q.StagnationDays).InclusiveBetween(30, 730);
            RuleFor(q => q.MaxContracts).InclusiveBetween(10, 500);
            RuleFor(q => q.TopVolatileCount).InclusiveBetween(1, 50);
            RuleFor(q => q.TopStableCount).InclusiveBetween(1, 50);
        }
    }
}
