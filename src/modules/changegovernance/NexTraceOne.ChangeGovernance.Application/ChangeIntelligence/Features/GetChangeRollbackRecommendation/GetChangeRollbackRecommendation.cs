using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeRollbackRecommendation;

/// <summary>
/// Feature: GetChangeRollbackRecommendation — computa um score de urgência de rollback
/// para uma release específica, combinando change confidence score, blast radius e
/// integridade de evidence packs.
///
/// Urgência de rollback:
/// - None (score 0–24):     rollback não indicado; confiança elevada ou impacto mínimo
/// - Suggest (score 25–49): considerar rollback; sinais de deterioração, impacto limitado
/// - Recommend (score 50–74): rollback recomendado; múltiplos sinais negativos
/// - Critical (score 75–100): rollback urgente; confiança baixa + alto blast radius
///
/// Relatório puro (query sem side effects) adequado para uso em dashboards e gates.
/// Wave J.3 — Change Rollback Recommendation.
/// </summary>
public static class GetChangeRollbackRecommendation
{
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IChangeConfidenceBreakdownRepository confidenceRepository,
        IBlastRadiusRepository blastRadiusRepository,
        IEvidencePackRepository evidencePackRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);
            if (release is null)
                return Error.NotFound("release.notFound", $"Release '{request.ReleaseId}' not found.");

            // Recolher sinais
            var confidence = await confidenceRepository.GetByReleaseIdAsync(releaseId, cancellationToken);
            var blastRadius = await blastRadiusRepository.GetByReleaseIdAsync(releaseId, cancellationToken);
            var evidencePacks = await evidencePackRepository.ListByReleaseIdsAsync(
                [request.ReleaseId], cancellationToken);

            var factors = new List<RollbackFactor>();
            var rollbackScore = 0;

            // Factor 1: Change Confidence Score (baixo → maior urgência)
            if (confidence is not null)
            {
                var confidenceScore = (int)confidence.AggregatedScore;
                int penalty;
                string confidenceNote;

                if (confidenceScore < 30)
                {
                    penalty = 40;
                    confidenceNote = $"Very low change confidence ({confidenceScore}/100) — high regression probability.";
                }
                else if (confidenceScore < 50)
                {
                    penalty = 25;
                    confidenceNote = $"Low change confidence ({confidenceScore}/100) — elevated risk of regression.";
                }
                else if (confidenceScore < 70)
                {
                    penalty = 10;
                    confidenceNote = $"Moderate change confidence ({confidenceScore}/100) — some risk indicators present.";
                }
                else
                {
                    penalty = 0;
                    confidenceNote = $"Adequate change confidence ({confidenceScore}/100) — rollback not urgently indicated by this factor.";
                }

                rollbackScore += penalty;
                factors.Add(new RollbackFactor("ChangeConfidence", penalty, confidenceNote));
            }
            else
            {
                // Sem dados de confiança = sinal de alerta moderado
                rollbackScore += 15;
                factors.Add(new RollbackFactor("ChangeConfidence", 15,
                    "Change confidence score not available — treating as moderate risk."));
            }

            // Factor 2: Blast Radius (muitos consumidores afetados → maior urgência)
            if (blastRadius is not null)
            {
                var affected = blastRadius.TotalAffectedConsumers;
                int penalty;
                string blastNote;

                if (affected >= 20)
                {
                    penalty = 30;
                    blastNote = $"High blast radius: {affected} consumers affected (direct + transitive). Rollback impact is wide.";
                }
                else if (affected >= 5)
                {
                    penalty = 15;
                    blastNote = $"Moderate blast radius: {affected} consumers affected.";
                }
                else
                {
                    penalty = 5;
                    blastNote = $"Low blast radius: {affected} consumers affected.";
                }

                rollbackScore += penalty;
                factors.Add(new RollbackFactor("BlastRadius", penalty, blastNote));
            }
            else
            {
                rollbackScore += 10;
                factors.Add(new RollbackFactor("BlastRadius", 10,
                    "Blast radius data not available — treating as moderate risk."));
            }

            // Factor 3: Evidence Pack Integrity
            if (evidencePacks.Count > 0)
            {
                var unsignedCount = evidencePacks.Count(e => !e.IsIntegritySigned);
                if (unsignedCount > 0)
                {
                    var penalty = Math.Min(30, unsignedCount * 10);
                    rollbackScore += penalty;
                    factors.Add(new RollbackFactor("EvidenceIntegrity", penalty,
                        $"{unsignedCount}/{evidencePacks.Count} evidence packs are not cryptographically signed — integrity unverified."));
                }
                else
                {
                    factors.Add(new RollbackFactor("EvidenceIntegrity", 0,
                        $"All {evidencePacks.Count} evidence packs are signed and integrity-verified."));
                }
            }
            else
            {
                rollbackScore += 10;
                factors.Add(new RollbackFactor("EvidenceIntegrity", 10,
                    "No evidence packs found for this release — change evidence is absent."));
            }

            rollbackScore = Math.Clamp(rollbackScore, 0, 100);
            var urgency = ClassifyUrgency(rollbackScore);

            return Result<Response>.Success(new Response(
                ReleaseId: request.ReleaseId,
                ServiceName: release.ServiceName,
                Environment: release.Environment,
                RollbackScore: rollbackScore,
                Urgency: urgency,
                Factors: factors,
                HasConfidenceData: confidence is not null,
                HasBlastRadiusData: blastRadius is not null,
                EvidencePackCount: evidencePacks.Count));
        }

        private static RollbackUrgency ClassifyUrgency(int score) => score switch
        {
            < 25 => RollbackUrgency.None,
            < 50 => RollbackUrgency.Suggest,
            < 75 => RollbackUrgency.Recommend,
            _    => RollbackUrgency.Critical,
        };
    }

    public enum RollbackUrgency
    {
        /// <summary>Rollback não indicado; confiança elevada e impacto mínimo.</summary>
        None,

        /// <summary>Considerar rollback; sinais de deterioração mas impacto limitado.</summary>
        Suggest,

        /// <summary>Rollback recomendado; múltiplos sinais negativos convergentes.</summary>
        Recommend,

        /// <summary>Rollback urgente; confiança muito baixa e/ou alto blast radius.</summary>
        Critical,
    }

    public sealed record RollbackFactor(
        string FactorName,
        int ScorePenalty,
        string Note);

    public sealed record Response(
        Guid ReleaseId,
        string ServiceName,
        string Environment,
        int RollbackScore,
        RollbackUrgency Urgency,
        IReadOnlyList<RollbackFactor> Factors,
        bool HasConfidenceData,
        bool HasBlastRadiusData,
        int EvidencePackCount);
}
