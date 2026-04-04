using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetCanaryRolloutStatus;

/// <summary>
/// Feature: GetCanaryRolloutStatus — obtém o estado actual do canary deployment para uma release.
/// Calcula um score de confiança baseado na percentagem de rollout:
///   100%  e promoted → ConfidenceBoost: "High"     — rollout completo, sem aborte
///   >= 50%           → ConfidenceBoost: "Medium"   — maioria do tráfego validou a versão
///   >= 10%           → ConfidenceBoost: "Low"       — validação parcial
///   aborted          → ConfidenceBoost: "Negative"  — canary foi revertido
///   sem dados        → ConfidenceBoost: "Unknown"
///
/// Valor: integrar canary rollout % como fator de confiança no advisory —
/// releases com canary avançado têm mais evidência real de estabilidade.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetCanaryRolloutStatus
{
    /// <summary>Query para obter o estado de canary rollout de uma release.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que obtém o registo mais recente de canary rollout e calcula o boost de confiança.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        ICanaryRolloutRepository canaryRolloutRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);
            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var latest = await canaryRolloutRepository.GetLatestByReleaseIdAsync(releaseId, cancellationToken);
            var history = await canaryRolloutRepository.ListByReleaseIdAsync(releaseId, cancellationToken);

            if (latest is null)
            {
                return new Response(
                    ReleaseId: request.ReleaseId,
                    HasData: false,
                    CurrentRolloutPercentage: 0m,
                    ActiveInstances: 0,
                    TotalInstances: 0,
                    SourceSystem: null,
                    IsPromoted: false,
                    IsAborted: false,
                    ConfidenceBoost: "Unknown",
                    ConfidenceRationale: "No canary rollout data has been recorded for this release.",
                    SnapshotCount: 0,
                    LatestRecordedAt: null);
            }

            var (boost, rationale) = DetermineConfidenceBoost(latest);

            return new Response(
                ReleaseId: request.ReleaseId,
                HasData: true,
                CurrentRolloutPercentage: latest.RolloutPercentage,
                ActiveInstances: latest.ActiveInstances,
                TotalInstances: latest.TotalInstances,
                SourceSystem: latest.SourceSystem,
                IsPromoted: latest.IsPromoted,
                IsAborted: latest.IsAborted,
                ConfidenceBoost: boost,
                ConfidenceRationale: rationale,
                SnapshotCount: history.Count,
                LatestRecordedAt: latest.RecordedAt);
        }

        private static (string Boost, string Rationale) DetermineConfidenceBoost(CanaryRollout rollout)
        {
            if (rollout.IsAborted)
                return ("Negative",
                    "Canary deployment was aborted. This release was rolled back during canary phase.");

            if (rollout.IsPromoted || rollout.RolloutPercentage >= 100m)
                return ("High",
                    $"Canary deployment fully promoted at {rollout.RolloutPercentage:F0}% rollout. " +
                    "Full traffic validation provides high confidence.");

            if (rollout.RolloutPercentage >= 50m)
                return ("Medium",
                    $"Canary deployment at {rollout.RolloutPercentage:F0}% rollout. " +
                    "Majority of traffic has been validated.");

            if (rollout.RolloutPercentage >= 10m)
                return ("Low",
                    $"Canary deployment at {rollout.RolloutPercentage:F0}% rollout. " +
                    "Partial traffic validation only.");

            return ("Minimal",
                $"Canary deployment at {rollout.RolloutPercentage:F0}% rollout. " +
                "Very limited traffic validation so far.");
        }
    }

    /// <summary>Resposta do estado de canary rollout da release.</summary>
    public sealed record Response(
        Guid ReleaseId,
        bool HasData,
        decimal CurrentRolloutPercentage,
        int ActiveInstances,
        int TotalInstances,
        string? SourceSystem,
        bool IsPromoted,
        bool IsAborted,
        string ConfidenceBoost,
        string ConfidenceRationale,
        int SnapshotCount,
        DateTimeOffset? LatestRecordedAt);
}
