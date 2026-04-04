using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeAdvisory;

/// <summary>
/// Feature: GetChangeAdvisory — gera uma recomendação de governança para uma release
/// com base em evidências, blast radius, score, rollback readiness e padrão histórico.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetChangeAdvisory
{
    private const int HistoricalLookbackDays = 90;
    private const int HistoricalMaxSamples = 50;
    private const int HistoricalMinSamplesForSignal = 5;

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
    /// Handler que agrega score, blast radius, rollback assessment e padrão histórico
    /// para produzir uma recomendação de confiança sobre a mudança.
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

            var similarReleases = await releaseRepository.ListSimilarReleasesAsync(
                excludeReleaseId: releaseId,
                serviceName: release.ServiceName,
                environment: release.Environment,
                changeLevel: release.ChangeLevel,
                from: release.CreatedAt.AddDays(-HistoricalLookbackDays),
                to: release.CreatedAt,
                maxResults: HistoricalMaxSamples,
                cancellationToken: cancellationToken);

            var factors = BuildFactors(score, blastRadius, rollback, release, similarReleases);
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
            Release release,
            IReadOnlyList<Release> similarReleases)
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
                0.20m));

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
                    0.20m));
            }
            else
            {
                factors.Add(new AdvisoryFactorDto(
                    "BlastRadiusScope", "Unknown",
                    "Blast radius has not been calculated.", 0.20m));
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
                    0.20m));
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
                    0.20m));
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
                    0.20m));
            }
            else
            {
                factors.Add(new AdvisoryFactorDto(
                    "RollbackReadiness", "Unknown",
                    "Rollback assessment has not been performed.", 0.20m));
            }

            // Historical pattern risk (5th factor — Change Confidence Score V2)
            factors.Add(BuildHistoricalPatternFactor(similarReleases, release));

            return factors;
        }

        private static AdvisoryFactorDto BuildHistoricalPatternFactor(
            IReadOnlyList<Release> similarReleases,
            Release release)
        {
            if (similarReleases.Count < HistoricalMinSamplesForSignal)
                return new AdvisoryFactorDto(
                    "HistoricalPattern",
                    "Unknown",
                    $"Only {similarReleases.Count} similar release(s) found in the last {HistoricalLookbackDays} days — " +
                    "insufficient data for a historical risk signal.",
                    0.20m);

            var total = similarReleases.Count;
            var adverseCount = similarReleases.Count(r =>
                r.Status is DeploymentStatus.RolledBack or DeploymentStatus.Failed);
            var adverseRate = (decimal)adverseCount / total;

            var (status, description) = adverseRate switch
            {
                >= 0.50m => ("Fail",
                    $"{adverseRate:P0} of {total} similar past {release.ChangeLevel} changes " +
                    $"in {release.Environment} resulted in rollback or failure. High historical risk."),
                >= 0.25m => ("Warning",
                    $"{adverseRate:P0} of {total} similar past {release.ChangeLevel} changes " +
                    $"in {release.Environment} resulted in rollback or failure. Moderate historical risk."),
                _ => ("Pass",
                    $"{((decimal)(total - adverseCount) / total):P0} success rate on {total} similar past " +
                    $"{release.ChangeLevel} changes in {release.Environment}. Low historical risk.")
            };

            return new AdvisoryFactorDto("HistoricalPattern", status, description, 0.20m);
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
            // "Unknown" from insufficient historical data is treated as neutral — does not block approval.
            var allPassOrUnknown = factors.All(f => f.Status is "Pass" or "Unknown");

            if (hasFail)
                return "Reject";

            if (unknownCount >= 2)
                return "NeedsMoreEvidence";

            if (changeScore <= 0.3m && allPassOrUnknown)
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
