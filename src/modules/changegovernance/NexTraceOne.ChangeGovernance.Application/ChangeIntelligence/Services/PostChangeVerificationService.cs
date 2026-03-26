using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Services;

/// <summary>
/// Serviço determinístico de verificação pós-mudança (P5.5).
///
/// MODELO DE COMPARAÇÃO:
///
/// 1. ErrorRate delta:    (observed - baseline) / max(baseline, 0.001)
///    - Degradação grave se delta &gt; +100% (ex: 0.01 → 0.02)
///    - Degradação leve  se delta &gt; +30%
///    - Melhoria         se delta &lt; -10%
///
/// 2. AvgLatency delta:  (observed - baseline) / max(baseline, 1)
///    - Degradação grave se delta &gt; +30%
///    - Degradação leve  se delta &gt; +15%
///    - Melhoria         se delta &lt; -10%
///
/// 3. P95Latency delta:  mesma lógica que AvgLatency, com thresholds iguais.
///
/// OUTCOME:
/// - Negative        se pelo menos 1 métrica com degradação grave
/// - NeedsAttention  se pelo menos 1 métrica com degradação leve (sem graves)
/// - Positive        se pelo menos 1 melhoria e nenhuma degradação
/// - Neutral         nenhuma variação relevante
///
/// CONFIDENCE (baseada na fase de observação):
/// - InitialObservation  → 0.30 base
/// - PreliminaryReview   → 0.60 base
/// - ConsolidatedReview  → 0.80 base
/// - FinalReview         → 0.90 base
/// Ajustada +0.10 quando todas as métricas estão consistentes.
/// </summary>
public sealed class PostChangeVerificationService : IPostChangeVerificationService
{
    private const decimal SevereErrorRateThreshold = 1.00m;   // +100% relativo
    private const decimal MildErrorRateThreshold = 0.30m;     // +30% relativo
    private const decimal ImprovementThreshold = -0.10m;      // -10% relativo
    private const decimal SevereLatencyThreshold = 0.30m;     // +30% relativo
    private const decimal MildLatencyThreshold = 0.15m;       // +15% relativo

    /// <inheritdoc />
    public VerificationResult Compare(
        ReleaseBaseline baseline,
        ObservationWindow observedWindow,
        ObservationPhase targetPhase)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(observedWindow);

        if (!observedWindow.IsCollected)
        {
            return new VerificationResult(
                ReviewOutcome.Inconclusive,
                0m,
                "Observation window metrics have not yet been collected.",
                0m, 0m, 0m);
        }

        var obsErrorRate = observedWindow.ErrorRate!.Value;
        var obsAvgLatency = observedWindow.AvgLatencyMs!.Value;
        var obsP95Latency = observedWindow.P95LatencyMs!.Value;

        var errorRateDelta = RelativeDelta(obsErrorRate, baseline.ErrorRate, 0.001m);
        var avgLatencyDelta = RelativeDelta(obsAvgLatency, baseline.AvgLatencyMs, 1m);
        var p95LatencyDelta = RelativeDelta(obsP95Latency, baseline.P95LatencyMs, 1m);

        // ── Classify outcome ──────────────────────────────────────────────────
        var hasSevereDegradation =
            errorRateDelta > SevereErrorRateThreshold ||
            avgLatencyDelta > SevereLatencyThreshold ||
            p95LatencyDelta > SevereLatencyThreshold;

        var hasMildDegradation =
            errorRateDelta > MildErrorRateThreshold ||
            avgLatencyDelta > MildLatencyThreshold ||
            p95LatencyDelta > MildLatencyThreshold;

        var hasImprovement =
            errorRateDelta < ImprovementThreshold &&
            avgLatencyDelta < ImprovementThreshold;

        var outcome = hasSevereDegradation ? ReviewOutcome.Negative
            : hasMildDegradation ? ReviewOutcome.NeedsAttention
            : hasImprovement ? ReviewOutcome.Positive
            : ReviewOutcome.Neutral;

        // ── Compute confidence ────────────────────────────────────────────────
        var baseConfidence = targetPhase switch
        {
            ObservationPhase.InitialObservation => 0.30m,
            ObservationPhase.PreliminaryReview => 0.60m,
            ObservationPhase.ConsolidatedReview => 0.80m,
            ObservationPhase.FinalReview => 0.90m,
            _ => 0.20m
        };

        // Boost confidence if all metrics point consistently in the same direction
        var consistent = (hasSevereDegradation || hasMildDegradation || hasImprovement ||
            (!hasSevereDegradation && !hasMildDegradation && !hasImprovement));
        var confidence = consistent
            ? Math.Min(baseConfidence + 0.10m, 1.0m)
            : baseConfidence;

        // ── Build summary ─────────────────────────────────────────────────────
        var summary = BuildSummary(outcome, errorRateDelta, avgLatencyDelta, p95LatencyDelta, targetPhase);

        return new VerificationResult(outcome, confidence, summary, errorRateDelta, avgLatencyDelta, p95LatencyDelta);
    }

    private static decimal RelativeDelta(decimal observed, decimal baseline, decimal minBaseline)
    {
        var safe = Math.Max(Math.Abs(baseline), minBaseline);
        return (observed - baseline) / safe;
    }

    private static string BuildSummary(
        ReviewOutcome outcome,
        decimal errorRateDelta,
        decimal avgLatencyDelta,
        decimal p95LatencyDelta,
        ObservationPhase phase)
    {
        var phaseName = phase.ToString();
        var errorPct = Math.Round(errorRateDelta * 100, 1);
        var avgPct = Math.Round(avgLatencyDelta * 100, 1);
        var p95Pct = Math.Round(p95LatencyDelta * 100, 1);

        return outcome switch
        {
            ReviewOutcome.Negative =>
                $"[{phaseName}] Severe degradation detected: " +
                $"error rate {errorPct:+0.0;-0.0}%, avg latency {avgPct:+0.0;-0.0}%, p95 latency {p95Pct:+0.0;-0.0}%.",
            ReviewOutcome.NeedsAttention =>
                $"[{phaseName}] Mild degradation observed: " +
                $"error rate {errorPct:+0.0;-0.0}%, avg latency {avgPct:+0.0;-0.0}%, p95 latency {p95Pct:+0.0;-0.0}%.",
            ReviewOutcome.Positive =>
                $"[{phaseName}] Release improved indicators: " +
                $"error rate {errorPct:+0.0;-0.0}%, avg latency {avgPct:+0.0;-0.0}%.",
            ReviewOutcome.Neutral =>
                $"[{phaseName}] No significant deviation from baseline. " +
                $"Error rate {errorPct:+0.0;-0.0}%, avg latency {avgPct:+0.0;-0.0}%.",
            _ =>
                $"[{phaseName}] Insufficient data to classify outcome."
        };
    }
}
