using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>
/// Contrato do serviço de verificação pós-mudança.
/// Compara as métricas de uma ObservationWindow com o baseline capturado antes do deploy
/// e determina o outcome e a confiança da review.
/// </summary>
public interface IPostChangeVerificationService
{
    /// <summary>
    /// Compara as métricas observadas com o baseline de referência e retorna
    /// o resultado da verificação (outcome, confidence, summary).
    /// </summary>
    VerificationResult Compare(
        ReleaseBaseline baseline,
        ObservationWindow observedWindow,
        ObservationPhase targetPhase);
}

/// <summary>
/// Resultado da comparação baseline vs observado.
/// Gerado pelo IPostChangeVerificationService para alimentar o PostReleaseReview.
/// </summary>
public sealed record VerificationResult(
    ReviewOutcome Outcome,
    decimal ConfidenceScore,
    string Summary,
    decimal ErrorRateDelta,
    decimal AvgLatencyDelta,
    decimal P95LatencyDelta);
