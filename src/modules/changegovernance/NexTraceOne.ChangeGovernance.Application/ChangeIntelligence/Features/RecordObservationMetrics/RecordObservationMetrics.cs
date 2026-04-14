using Ardalis.GuardClauses;

using FluentValidation;

using MediatR;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordObservationMetrics;

/// <summary>
/// Feature: RecordObservationMetrics — registra as métricas observadas de uma janela pós-release,
/// compara automaticamente contra o baseline e progride a PostReleaseReview.
///
/// Este é o pipeline automático de post-change verification (P5.5):
///   1. Cria ou obtém a ObservationWindow para a fase solicitada
///   2. Registra as métricas observadas na janela
///   3. Consulta o baseline da release
///   4. Compara métricas usando PostChangeVerificationService
///   5. Inicia ou progride a PostReleaseReview com o resultado
///
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RecordObservationMetrics
{
    /// <summary>
    /// Comando para registar as métricas observadas numa janela de observação pós-release
    /// e desencadear automaticamente a comparação baseline vs observado.
    /// </summary>
    public sealed record Command(
        Guid ReleaseId,
        ObservationPhase Phase,
        DateTimeOffset WindowStartsAt,
        DateTimeOffset WindowEndsAt,
        decimal RequestsPerMinute,
        decimal ErrorRate,
        decimal AvgLatencyMs,
        decimal P95LatencyMs,
        decimal P99LatencyMs,
        decimal Throughput) : ICommand<Response>;

    /// <summary>Valida o comando de registo de métricas de observação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.Phase).IsInEnum();
            RuleFor(x => x.WindowEndsAt).GreaterThan(x => x.WindowStartsAt)
                .WithMessage("Observation window end must be after start.");
            RuleFor(x => x.RequestsPerMinute).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ErrorRate).InclusiveBetween(0m, 1m);
            RuleFor(x => x.AvgLatencyMs).GreaterThanOrEqualTo(0);
            RuleFor(x => x.P95LatencyMs).GreaterThanOrEqualTo(0);
            RuleFor(x => x.P99LatencyMs).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Throughput).GreaterThanOrEqualTo(0);
        }
    }

    /// <summary>
    /// Handler principal do pipeline de post-change verification.
    /// Orquestra: ObservationWindow → baseline lookup → comparison → PostReleaseReview progression.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IReleaseBaselineRepository baselineRepository,
        IObservationWindowRepository windowRepository,
        IPostReleaseReviewRepository reviewRepository,
        IPostChangeVerificationService verificationService,
        IChangeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);

            // ── 1. Validate release exists ────────────────────────────────────
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);
            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            // ── 2. Get baseline (required for comparison) ─────────────────────
            var baseline = await baselineRepository.GetByReleaseIdAsync(releaseId, cancellationToken);
            if (baseline is null)
                return ChangeIntelligenceErrors.BaselineNotFound(request.ReleaseId.ToString());

            // ── 3. Create or get ObservationWindow for this phase ─────────────
            var now = dateTimeProvider.UtcNow;

            var window = await windowRepository.GetByReleaseIdAndPhaseAsync(
                releaseId, request.Phase, cancellationToken);

            bool isNewWindow = window is null;
            if (isNewWindow)
            {
                window = ObservationWindow.Create(
                    releaseId,
                    request.Phase,
                    request.WindowStartsAt,
                    request.WindowEndsAt);

                windowRepository.Add(window);
            }

            // ── 4. Record observed metrics ────────────────────────────────────
            var recordResult = window!.RecordMetrics(
                request.RequestsPerMinute,
                request.ErrorRate,
                request.AvgLatencyMs,
                request.P95LatencyMs,
                request.P99LatencyMs,
                request.Throughput,
                now);

            if (recordResult.IsFailure)
                return recordResult.Error;

            // ── 5. Compare against baseline ───────────────────────────────────
            var verification = verificationService.Compare(baseline, window, request.Phase);

            // ── 6. Start or progress PostReleaseReview ────────────────────────
            var review = await reviewRepository.GetByReleaseIdAsync(releaseId, cancellationToken);

            bool isNewReview = review is null;
            if (isNewReview)
            {
                review = PostReleaseReview.Start(releaseId, now);
                reviewRepository.Add(review);
            }

            // When the review was just created it starts at InitialObservation.
            // If the requested phase is InitialObservation we record the outcome
            // in-place (no phase advancement). For subsequent phases we progress forward.
            Result<MediatR.Unit> updateResult;
            if (!isNewReview || request.Phase != ObservationPhase.InitialObservation)
            {
                updateResult = review!.Progress(
                    request.Phase,
                    verification.Outcome,
                    verification.ConfidenceScore,
                    verification.Summary,
                    now);
            }
            else
            {
                updateResult = review!.RecordInitialObservation(
                    verification.Outcome,
                    verification.ConfidenceScore,
                    verification.Summary);
            }

            if (updateResult.IsFailure)
                return updateResult.Error;

            if (!isNewReview)
                reviewRepository.Update(review);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                window.Id.Value,
                review.Id.Value,
                request.Phase.ToString(),
                verification.Outcome.ToString(),
                verification.ConfidenceScore,
                verification.Summary,
                verification.ErrorRateDelta,
                verification.AvgLatencyDelta,
                verification.P95LatencyDelta,
                review.IsCompleted,
                isNewWindow);
        }
    }

    /// <summary>Resposta do registo de métricas de observação e verificação pós-mudança.</summary>
    public sealed record Response(
        Guid ObservationWindowId,
        Guid ReviewId,
        string Phase,
        string Outcome,
        decimal ConfidenceScore,
        string Summary,
        decimal ErrorRateDelta,
        decimal AvgLatencyDelta,
        decimal P95LatencyDelta,
        bool ReviewCompleted,
        bool IsNewWindow);
}
