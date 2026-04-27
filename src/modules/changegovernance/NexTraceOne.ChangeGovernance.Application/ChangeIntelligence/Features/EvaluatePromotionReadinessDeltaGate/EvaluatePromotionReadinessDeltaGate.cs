using Ardalis.GuardClauses;

using FluentValidation;

using Microsoft.Extensions.Options;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using ReadinessDeltaFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetPromotionReadinessDelta.GetPromotionReadinessDelta;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.EvaluatePromotionReadinessDeltaGate;

/// <summary>
/// Feature: EvaluatePromotionReadinessDeltaGate — avalia se um serviço está pronto
/// para promoção com base nos deltas de runtime entre dois ambientes.
///
/// Comportamento por nível de readiness:
/// - <c>Ready</c>   → Gate passa sempre.
/// - <c>Review</c>  → Gate passa por defeito; bloqueia se <c>promotion.readiness_delta.block_on_review = true</c>.
/// - <c>Blocked</c> → Gate falha sempre.
/// - <c>Unknown</c> → Gate passa (dados insuficientes = não-bloqueante por defeito).
///
/// Integra com <see cref="IRuntimeComparisonReader"/> (via bridge OtelRuntimeComparisonReader)
/// sem referência direta ao módulo OperationalIntelligence.
/// </summary>
public static class EvaluatePromotionReadinessDeltaGate
{
    /// <summary>Janela máxima (em dias) aceite pela query.</summary>
    public const int MaxWindowDays = 60;

    /// <summary>Janela default quando o chamador não informa <c>WindowDays</c>.</summary>
    public const int DefaultWindowDays = 7;

    /// <summary>Query de avaliação do gate.</summary>
    public sealed record Query(
        string ServiceName,
        string EnvironmentFrom,
        string EnvironmentTo,
        int? WindowDays) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.EnvironmentFrom).NotEmpty().MaximumLength(100);
            RuleFor(x => x.EnvironmentTo).NotEmpty().MaximumLength(100);
            RuleFor(x => x.EnvironmentTo)
                .NotEqual(x => x.EnvironmentFrom)
                .WithMessage("EnvironmentTo must differ from EnvironmentFrom.");
            RuleFor(x => x.WindowDays!.Value)
                .InclusiveBetween(1, MaxWindowDays)
                .When(x => x.WindowDays.HasValue);
        }
    }

    /// <summary>Handler que avalia o gate de readiness de promoção.</summary>
    public sealed class Handler(
        IRuntimeComparisonReader reader,
        ICurrentTenant currentTenant,
        IOptions<PromotionReadinessDeltaOptions> options) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var window = request.WindowDays ?? DefaultWindowDays;

            var snapshot = await reader.CompareAsync(
                currentTenant.Id,
                request.ServiceName,
                request.EnvironmentFrom,
                request.EnvironmentTo,
                window,
                cancellationToken);

            var readiness = ComputeReadiness(snapshot);
            var (passed, reason) = EvaluateGate(readiness, snapshot, options.Value);

            return new Response(
                ServiceName: snapshot.ServiceName,
                EnvironmentFrom: snapshot.EnvironmentFrom,
                EnvironmentTo: snapshot.EnvironmentTo,
                WindowDays: snapshot.WindowDays,
                Passed: passed,
                Readiness: readiness,
                Reason: reason,
                DataQuality: snapshot.DataQuality,
                SimulatedNote: snapshot.SimulatedNote);
        }

        private static ReadinessDeltaFeature.PromotionReadinessLevel ComputeReadiness(
            RuntimeComparisonSnapshot snapshot)
        {
            if (snapshot.DataQuality <= 0m)
                return ReadinessDeltaFeature.PromotionReadinessLevel.Unknown;

            var error = snapshot.ErrorRateDelta;
            var latency = snapshot.LatencyP95DeltaMs;
            var incidents = snapshot.IncidentsDelta;

            if ((error.HasValue && error.Value > 0.05m)
                || (latency.HasValue && latency.Value > 250m)
                || (incidents.HasValue && incidents.Value > 0))
            {
                return ReadinessDeltaFeature.PromotionReadinessLevel.Blocked;
            }

            if ((error.HasValue && error.Value > 0.01m)
                || (latency.HasValue && latency.Value > 75m))
            {
                return ReadinessDeltaFeature.PromotionReadinessLevel.Review;
            }

            return ReadinessDeltaFeature.PromotionReadinessLevel.Ready;
        }

        private static (bool Passed, string Reason) EvaluateGate(
            ReadinessDeltaFeature.PromotionReadinessLevel readiness,
            RuntimeComparisonSnapshot snapshot,
            PromotionReadinessDeltaOptions opts)
        {
            return readiness switch
            {
                ReadinessDeltaFeature.PromotionReadinessLevel.Ready =>
                    (true, "Runtime delta is within acceptable thresholds."),

                ReadinessDeltaFeature.PromotionReadinessLevel.Review when opts.BlockOnReview =>
                    (false, BuildReviewReason(snapshot)),

                ReadinessDeltaFeature.PromotionReadinessLevel.Review =>
                    (true, $"Runtime delta requires review but gate is non-blocking. {BuildReviewReason(snapshot)}"),

                ReadinessDeltaFeature.PromotionReadinessLevel.Blocked =>
                    (false, BuildBlockedReason(snapshot)),

                ReadinessDeltaFeature.PromotionReadinessLevel.Unknown =>
                    (true, "Insufficient runtime data; gate is non-blocking when data quality is zero."),

                _ => (true, "Gate passed.")
            };
        }

        private static string BuildReviewReason(RuntimeComparisonSnapshot snapshot)
        {
            var parts = new List<string>();
            if (snapshot.ErrorRateDelta.HasValue && snapshot.ErrorRateDelta.Value > 0.01m)
                parts.Add($"error rate +{snapshot.ErrorRateDelta.Value:P1}");
            if (snapshot.LatencyP95DeltaMs.HasValue && snapshot.LatencyP95DeltaMs.Value > 75m)
                parts.Add($"p95 latency +{snapshot.LatencyP95DeltaMs.Value:F0}ms");
            return parts.Count > 0
                ? $"Moderate regression detected: {string.Join(", ", parts)}."
                : "Moderate runtime regression detected.";
        }

        private static string BuildBlockedReason(RuntimeComparisonSnapshot snapshot)
        {
            var parts = new List<string>();
            if (snapshot.ErrorRateDelta.HasValue && snapshot.ErrorRateDelta.Value > 0.05m)
                parts.Add($"error rate +{snapshot.ErrorRateDelta.Value:P1}");
            if (snapshot.LatencyP95DeltaMs.HasValue && snapshot.LatencyP95DeltaMs.Value > 250m)
                parts.Add($"p95 latency +{snapshot.LatencyP95DeltaMs.Value:F0}ms");
            if (snapshot.IncidentsDelta.HasValue && snapshot.IncidentsDelta.Value > 0)
                parts.Add($"{snapshot.IncidentsDelta.Value} new incident(s)");
            return parts.Count > 0
                ? $"Critical regression detected: {string.Join(", ", parts)}."
                : "Critical runtime regression detected.";
        }
    }

    /// <summary>Resposta do gate de readiness de promoção.</summary>
    public sealed record Response(
        string ServiceName,
        string EnvironmentFrom,
        string EnvironmentTo,
        int WindowDays,
        bool Passed,
        ReadinessDeltaFeature.PromotionReadinessLevel Readiness,
        string Reason,
        decimal DataQuality,
        string? SimulatedNote);
}

/// <summary>Opções de configuração do gate de readiness de promoção.</summary>
public sealed class PromotionReadinessDeltaOptions
{
    public const string SectionKey = "promotion";

    /// <summary>
    /// Quando true, o nível <c>Review</c> bloqueia a promoção (gate falha).
    /// Quando false (default), o nível <c>Review</c> passa com aviso.
    /// Chave: <c>promotion.readiness_delta.block_on_review</c>.
    /// </summary>
    public bool BlockOnReview { get; set; } = false;
}
