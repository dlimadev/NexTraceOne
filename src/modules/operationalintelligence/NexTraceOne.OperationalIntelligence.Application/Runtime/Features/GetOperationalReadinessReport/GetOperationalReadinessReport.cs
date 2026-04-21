using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetOperationalReadinessReport;

/// <summary>
/// Feature: GetOperationalReadinessReport — relatório de prontidão operacional pré-produção.
///
/// Avalia um serviço num ambiente específico contra múltiplas dimensões operacionais:
/// - SLO Compliance: proporção de observações de SLO em estado "Met" (não "Breached")
/// - Chaos Resilience: proporção de experimentos de chaos concluídos com sucesso
/// - Profiling Coverage: existência de sessão de profiling recente para o serviço
/// - Drift Free: ausência de drift findings não reconhecidos no serviço/ambiente
/// - Baseline Coverage: existência de snapshot de runtime recente
///
/// Produz um score composto (0–100) e uma classificação de readiness:
/// - ReadyForProduction (score ≥ 80 e sem bloqueadores)
/// - ConditionallyReady (score ≥ 60, sem SLO breaches bloqueadoras)
/// - NotReady (score &lt; 60 ou dimensões críticas falham)
///
/// Orientado para Tech Lead, Platform Admin e Engineer personas.
/// Suporta gates de promoção pré-produção e decisões de deployment.
///
/// Wave L.3 — Operational Readiness Report (OperationalIntelligence).
/// </summary>
public static class GetOperationalReadinessReport
{
    /// <summary>
    /// <para><c>ServiceName</c>: nome do serviço a avaliar.</para>
    /// <para><c>TargetEnvironment</c>: ambiente de destino (tipicamente "production").</para>
    /// <para><c>SourceEnvironment</c>: ambiente de origem para SLO e profiling (tipicamente "staging" ou "pre-production").</para>
    /// <para><c>LookbackDays</c>: janela de avaliação para SLO e chaos (1–90, default 30).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        string ServiceName,
        string SourceEnvironment,
        string TargetEnvironment,
        int LookbackDays = 30) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.SourceEnvironment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TargetEnvironment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LookbackDays).InclusiveBetween(1, 90);
        }
    }

    public sealed class Handler(
        ISloObservationRepository sloRepository,
        IChaosExperimentRepository chaosRepository,
        IProfilingSessionRepository profilingRepository,
        IDriftFindingRepository driftRepository,
        IRuntimeSnapshotRepository snapshotRepository,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;
            var since = now.AddDays(-request.LookbackDays);

            // ── Dimensão 1: SLO Compliance ────────────────────────────────────
            var sloObservations = await sloRepository.ListByServiceAsync(
                request.TenantId,
                request.ServiceName,
                since,
                now,
                request.SourceEnvironment,
                cancellationToken);

            var totalSlos = sloObservations.Count;
            var sloMetCount = sloObservations.Count(o => o.Status == SloObservationStatus.Met);
            var sloBreachedCount = sloObservations.Count(o => o.Status == SloObservationStatus.Breached);
            var sloComplianceRate = totalSlos > 0
                ? Math.Round((decimal)sloMetCount / totalSlos * 100, 1)
                : 100m;
            var sloScore = totalSlos > 0
                ? Math.Round(sloComplianceRate, 0)
                : 60m; // neutral when no data

            // ── Dimensão 2: Chaos Resilience ─────────────────────────────────
            var allExperiments = await chaosRepository.ListAsync(
                request.TenantId,
                request.ServiceName,
                null,
                null,
                cancellationToken);
            var recentExperiments = allExperiments
                .Where(e => e.CreatedAt >= since)
                .ToList();
            var chaosTotal = recentExperiments.Count;
            var chaosCompleted = recentExperiments.Count(e => e.Status == ExperimentStatus.Completed);
            var chaosFailed = recentExperiments.Count(e => e.Status == ExperimentStatus.Failed);
            var chaosSuccessRate = (chaosCompleted + chaosFailed) > 0
                ? Math.Round((decimal)chaosCompleted / (chaosCompleted + chaosFailed) * 100, 1)
                : 100m;
            var chaosScore = chaosTotal > 0 ? Math.Round(chaosSuccessRate, 0) : 60m;

            // ── Dimensão 3: Profiling Coverage ────────────────────────────────
            var latestProfiling = await profilingRepository.GetLatestByServiceAsync(
                request.ServiceName,
                request.SourceEnvironment,
                cancellationToken);
            var hasRecentProfiling = latestProfiling is not null
                && latestProfiling.WindowStart >= since;
            var profilingScore = hasRecentProfiling ? 100m : 40m;

            // ── Dimensão 4: Drift Free ─────────────────────────────────────────
            // Unacknowledged drift findings indicate instability between environments
            var unacknowledgedDrift = await driftRepository.ListUnacknowledgedAsync(1, 50, cancellationToken);
            var serviceDrift = unacknowledgedDrift
                .Where(d => d.ServiceName.Equals(request.ServiceName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var isDriftFree = serviceDrift.Count == 0;
            var driftScore = isDriftFree ? 100m : Math.Max(0m, 100m - serviceDrift.Count * 25m);

            // ── Dimensão 5: Baseline Coverage ─────────────────────────────────
            var latestSnapshot = await snapshotRepository.GetLatestByServiceAsync(
                request.ServiceName,
                request.SourceEnvironment,
                cancellationToken);
            var hasRecentBaseline = latestSnapshot is not null
                && latestSnapshot.CapturedAt >= since;
            var baselineScore = hasRecentBaseline ? 100m : 40m;

            // ── Score composto ─────────────────────────────────────────────────
            // Pesos: SLO (35%), Chaos (25%), Drift (20%), Profiling (10%), Baseline (10%)
            var compositeScore = Math.Round(
                sloScore * 0.35m +
                chaosScore * 0.25m +
                driftScore * 0.20m +
                profilingScore * 0.10m +
                baselineScore * 0.10m,
                1);

            var blockers = BuildBlockers(
                sloBreachedCount,
                chaosFailed,
                isDriftFree,
                hasRecentProfiling,
                hasRecentBaseline,
                request.LookbackDays);

            var classification = ClassifyReadiness(compositeScore, blockers);

            var dimensions = new ReadinessDimensions(
                SloComplianceRate: sloComplianceRate,
                SloScore: sloScore,
                TotalSloObservations: totalSlos,
                SloBreachedCount: sloBreachedCount,
                ChaosSuccessRate: chaosSuccessRate,
                ChaosScore: chaosScore,
                TotalChaosExperiments: chaosTotal,
                ChaosFailed: chaosFailed,
                HasRecentProfiling: hasRecentProfiling,
                ProfilingScore: profilingScore,
                IsDriftFree: isDriftFree,
                DriftScore: driftScore,
                UnacknowledgedDriftCount: serviceDrift.Count,
                HasRecentBaseline: hasRecentBaseline,
                BaselineScore: baselineScore);

            return Result<Response>.Success(new Response(
                GeneratedAt: now,
                TenantId: request.TenantId,
                ServiceName: request.ServiceName,
                SourceEnvironment: request.SourceEnvironment,
                TargetEnvironment: request.TargetEnvironment,
                LookbackDays: request.LookbackDays,
                CompositeScore: compositeScore,
                Classification: classification,
                Blockers: blockers,
                Dimensions: dimensions));
        }

        private static IReadOnlyList<string> BuildBlockers(
            int sloBreachedCount,
            int chaosFailed,
            bool isDriftFree,
            bool hasRecentProfiling,
            bool hasRecentBaseline,
            int lookbackDays)
        {
            var blockers = new List<string>();
            if (sloBreachedCount > 0)
                blockers.Add($"{sloBreachedCount} SLO breach(es) detected in the last {lookbackDays} days. Resolve before promoting to production.");
            if (chaosFailed > 0)
                blockers.Add($"{chaosFailed} chaos experiment(s) failed recently. Review resilience gaps before promotion.");
            if (!isDriftFree)
                blockers.Add("Unacknowledged environment drift detected. Investigate configuration inconsistencies.");
            if (!hasRecentProfiling)
                blockers.Add($"No profiling session found in the last {lookbackDays} days. Run continuous profiling before promotion.");
            if (!hasRecentBaseline)
                blockers.Add($"No runtime baseline captured in the last {lookbackDays} days. Establish baseline before production deployment.");
            return blockers;
        }

        private static ReadinessClassification ClassifyReadiness(
            decimal score,
            IReadOnlyList<string> blockers) =>
            score >= 80m && blockers.Count == 0
                ? ReadinessClassification.ReadyForProduction
                : score >= 60m
                    ? ReadinessClassification.ConditionallyReady
                    : ReadinessClassification.NotReady;
    }

    // ── Enums ────────────────────────────────────────────────────────────

    /// <summary>Classificação de prontidão operacional para produção.</summary>
    public enum ReadinessClassification
    {
        /// <summary>Todas as dimensões atendem os thresholds. Pronto para produção.</summary>
        ReadyForProduction = 0,

        /// <summary>Score ≥ 60 mas existem bloqueadores que requerem atenção.</summary>
        ConditionallyReady = 1,

        /// <summary>Score &lt; 60 ou bloqueadores críticos impedem a promoção.</summary>
        NotReady = 2,
    }

    // ── Response DTOs ────────────────────────────────────────────────────

    public sealed record ReadinessDimensions(
        decimal SloComplianceRate,
        decimal SloScore,
        int TotalSloObservations,
        int SloBreachedCount,
        decimal ChaosSuccessRate,
        decimal ChaosScore,
        int TotalChaosExperiments,
        int ChaosFailed,
        bool HasRecentProfiling,
        decimal ProfilingScore,
        bool IsDriftFree,
        decimal DriftScore,
        int UnacknowledgedDriftCount,
        bool HasRecentBaseline,
        decimal BaselineScore);

    public sealed record Response(
        DateTimeOffset GeneratedAt,
        string TenantId,
        string ServiceName,
        string SourceEnvironment,
        string TargetEnvironment,
        int LookbackDays,
        decimal CompositeScore,
        ReadinessClassification Classification,
        IReadOnlyList<string> Blockers,
        ReadinessDimensions Dimensions);
}
