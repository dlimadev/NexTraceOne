using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NexTraceOne.ChangeIntelligence.Domain.Errors;

namespace NexTraceOne.ChangeIntelligence.Application.Features.GetChangeAdvisory;

/// <summary>
/// Feature: GetChangeAdvisory — gera uma recomendação de governança para uma release
/// com base em evidências, blast radius, score e rollback readiness.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetChangeAdvisory
{
    /// <summary>Query para obter a recomendação de governança de uma release.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de advisory.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que agrega score, blast radius e rollback assessment para produzir
    /// uma recomendação de confiança sobre a mudança.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IChangeScoreRepository scoreRepository,
        IBlastRadiusRepository blastRadiusRepository,
        IRollbackAssessmentRepository rollbackRepository,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);

            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var score = await scoreRepository.GetByReleaseIdAsync(releaseId, cancellationToken);
            var blastRadius = await blastRadiusRepository.GetByReleaseIdAsync(releaseId, cancellationToken);
            var rollback = await rollbackRepository.GetByReleaseIdAsync(releaseId, cancellationToken);

            var factors = BuildFactors(score, blastRadius, rollback, release);
            var overallConfidence = ComputeOverallConfidence(factors);
            var recommendation = DetermineRecommendation(factors, release.ChangeScore);
            var rationale = BuildRationale(recommendation, factors, release.ChangeScore);

            return new Response(
                release.Id.Value,
                recommendation,
                rationale,
                overallConfidence,
                factors,
                dateTimeProvider.UtcNow);
        }

        private static List<AdvisoryFactorDto> BuildFactors(
            ChangeIntelligenceScore? score,
            BlastRadiusReport? blastRadius,
            RollbackAssessment? rollback,
            Release release)
        {
            var factors = new List<AdvisoryFactorDto>();

            // Evidence completeness
            var evidenceStatus = (score is not null && blastRadius is not null && rollback is not null)
                ? "Pass"
                : (score is null && blastRadius is null && rollback is null)
                    ? "Unknown"
                    : "Warning";
            factors.Add(new AdvisoryFactorDto(
                "EvidenceCompleteness",
                evidenceStatus,
                evidenceStatus == "Pass"
                    ? "All evidence sources are available."
                    : evidenceStatus == "Unknown"
                        ? "No evidence data is available for this release."
                        : "Some evidence sources are missing.",
                0.25m));

            // Blast radius scope
            if (blastRadius is not null)
            {
                var blastStatus = blastRadius.TotalAffectedConsumers switch
                {
                    0 => "Pass",
                    <= 5 => "Warning",
                    _ => "Fail"
                };
                factors.Add(new AdvisoryFactorDto(
                    "BlastRadiusScope",
                    blastStatus,
                    $"Total affected consumers: {blastRadius.TotalAffectedConsumers}.",
                    0.25m));
            }
            else
            {
                factors.Add(new AdvisoryFactorDto(
                    "BlastRadiusScope", "Unknown",
                    "Blast radius has not been calculated.", 0.25m));
            }

            // Change score
            if (score is not null)
            {
                var scoreStatus = score.Score switch
                {
                    <= 0.3m => "Pass",
                    <= 0.6m => "Warning",
                    _ => "Fail"
                };
                factors.Add(new AdvisoryFactorDto(
                    "ChangeScore",
                    scoreStatus,
                    $"Change score is {score.Score:F2}.",
                    0.25m));
            }
            else
            {
                var fallbackStatus = release.ChangeScore switch
                {
                    <= 0.3m => "Pass",
                    <= 0.6m => "Warning",
                    _ => "Fail"
                };
                factors.Add(new AdvisoryFactorDto(
                    "ChangeScore",
                    fallbackStatus,
                    $"Change score is {release.ChangeScore:F2} (from release record).",
                    0.25m));
            }

            // Rollback readiness
            if (rollback is not null)
            {
                var rollbackStatus = rollback.IsViable ? "Pass" : "Fail";
                factors.Add(new AdvisoryFactorDto(
                    "RollbackReadiness",
                    rollbackStatus,
                    rollback.IsViable
                        ? $"Rollback is viable with readiness score {rollback.ReadinessScore:F2}."
                        : $"Rollback is not viable. Recommendation: {rollback.Recommendation}.",
                    0.25m));
            }
            else
            {
                factors.Add(new AdvisoryFactorDto(
                    "RollbackReadiness", "Unknown",
                    "Rollback assessment has not been performed.", 0.25m));
            }

            return factors;
        }

        private static decimal ComputeOverallConfidence(IReadOnlyList<AdvisoryFactorDto> factors)
        {
            if (factors.Count == 0)
                return 0m;

            var totalWeight = factors.Sum(f => f.Weight ?? 0m);
            if (totalWeight == 0m)
                return 0m;

            var weightedSum = factors.Sum(f =>
            {
                var value = f.Status switch
                {
                    "Pass" => 1.0m,
                    "Warning" => 0.5m,
                    "Unknown" => 0.25m,
                    _ => 0m
                };
                return value * (f.Weight ?? 0m);
            });

            return Math.Round(weightedSum / totalWeight, 2);
        }

        private static string DetermineRecommendation(
            IReadOnlyList<AdvisoryFactorDto> factors,
            decimal changeScore)
        {
            var hasFail = factors.Any(f => f.Status == "Fail");
            var unknownCount = factors.Count(f => f.Status == "Unknown");
            var allPass = factors.All(f => f.Status == "Pass");

            if (hasFail)
                return "Reject";

            if (unknownCount >= 2)
                return "NeedsMoreEvidence";

            if (changeScore <= 0.3m && allPass)
                return "Approve";

            // Score above safe threshold or some factors have warnings — require conditional review
            return "ApproveConditionally";
        }

        private static string BuildRationale(
            string recommendation,
            IReadOnlyList<AdvisoryFactorDto> factors,
            decimal changeScore)
        {
            return recommendation switch
            {
                "Approve" =>
                    $"All factors pass and the change score ({changeScore:F2}) is within safe thresholds. The change is recommended for approval.",
                "Reject" =>
                    $"One or more factors have failed: {string.Join(", ", factors.Where(f => f.Status == "Fail").Select(f => f.FactorName))}. The change is not recommended for production.",
                "ApproveConditionally" =>
                    $"The change score is {changeScore:F2}. Conditional approval is recommended pending review of warning factors.",
                "NeedsMoreEvidence" =>
                    $"Insufficient evidence is available ({factors.Count(f => f.Status == "Unknown")} unknown factors). Please gather more data before proceeding.",
                _ => "Unable to determine recommendation."
            };
        }
    }

    /// <summary>DTO de fator individual da recomendação.</summary>
    public sealed record AdvisoryFactorDto(
        string FactorName,
        string Status,
        string Description,
        decimal? Weight);

    /// <summary>
    /// Resposta com a recomendação de governança da mudança.
    /// Inclui recomendação, confiança geral, factores e justificação.
    /// </summary>
    public sealed record Response(
        Guid ReleaseId,
        string Recommendation,
        string Rationale,
        decimal OverallConfidence,
        IReadOnlyList<AdvisoryFactorDto> Factors,
        DateTimeOffset GeneratedAt);
}
