using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetMaturityScorecards;

/// <summary>
/// Feature: GetMaturityScorecards — scorecards de maturidade por equipa ou domínio.
/// Computa maturidade real a partir de dados de Teams e cobertura de Governance Pack rollouts por equipa.
/// Dimensões ricas (ownership, runbook, etc.) requerem integração cross-module — parcialmente disponíveis.
/// </summary>
public static class GetMaturityScorecards
{
    /// <summary>Query de scorecards de maturidade. Dimensão: team (padrão) ou domain.</summary>
    public sealed record Query(
        string? Dimension = null) : IQuery<Response>;

    /// <summary>
    /// Handler que computa scorecards de maturidade com base nos dados reais de Teams
    /// e na cobertura de Governance Pack rollouts filtrados por equipa.
    /// </summary>
    public sealed class Handler(
        ITeamRepository teamRepository,
        IGovernancePackRepository packRepository,
        IGovernanceRolloutRecordRepository rolloutRepository,
        IGovernanceWaiverRepository waiverRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var dimension = request.Dimension ?? "team";

            var teams = await teamRepository.ListAsync(status: null, ct: cancellationToken);
            var packs = await packRepository.ListAsync(category: null, status: null, ct: cancellationToken);
            var rollouts = await rolloutRepository.ListAsync(
                packId: null, scopeType: null, scopeValue: null, status: null, ct: cancellationToken);
            var waivers = await waiverRepository.ListAsync(packId: null, status: null, ct: cancellationToken);

            var scorecards = teams
                .Select(team => BuildTeamScorecard(team, packs, rollouts, waivers))
                .OrderByDescending(s => s.OverallMaturity)
                .ToList();

            var response = new Response(
                Dimension: dimension,
                Scorecards: scorecards,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Result<Response>.Success(response);
        }

        private static MaturityScorecardDto BuildTeamScorecard(
            Team team,
            IReadOnlyList<GovernancePack> packs,
            IReadOnlyList<GovernanceRolloutRecord> rollouts,
            IReadOnlyList<GovernanceWaiver> waivers)
        {
            var teamId = team.Id.Value.ToString();

            // Rollouts específicos desta equipa
            var teamRollouts = rollouts.Where(r =>
                r.ScopeType == GovernanceScopeType.Team && r.Scope == teamId).ToList();
            var teamWaivers = waivers.Where(w =>
                w.ScopeType == GovernanceScopeType.Team && w.Scope == teamId).ToList();

            // Dimensões deriváveis de dados reais
            var contractPacks = packs.Where(p => p.Category == GovernanceRuleCategory.Contracts).ToList();
            var sourcePacks = packs.Where(p => p.Category == GovernanceRuleCategory.SourceOfTruth).ToList();
            var changePacks = packs.Where(p => p.Category == GovernanceRuleCategory.Changes).ToList();
            var operationsPacks = packs.Where(p => p.Category == GovernanceRuleCategory.Operations).ToList();
            var aiGovPacks = packs.Where(p => p.Category == GovernanceRuleCategory.AIGovernance).ToList();

            var contractScore = ComputeDimensionScore(contractPacks, teamRollouts, teamWaivers);
            var sourceScore = ComputeDimensionScore(sourcePacks, teamRollouts, teamWaivers);
            var changeScore = ComputeDimensionScore(changePacks, teamRollouts, teamWaivers);
            var operationsScore = ComputeDimensionScore(operationsPacks, teamRollouts, teamWaivers);
            var aiScore = ComputeDimensionScore(aiGovPacks, teamRollouts, teamWaivers);

            // Para dimensões sem packs específicos, usa média dos packs por equipa
            var totalPackScore = packs.Count == 0 ? 0m
                : (contractScore + sourceScore + changeScore + operationsScore + aiScore) / 5m;

            var dimensions = new List<MaturityDimensionScoreDto>
            {
                new("contract", ScoreToMaturity(contractScore), contractScore, 10m,
                    contractPacks.Count == 0
                        ? "No contract governance packs defined"
                        : $"{contractPacks.Count} contract governance pack(s) — rollout coverage {contractScore:F1}/10"),
                new("documentation", ScoreToMaturity(sourceScore), sourceScore, 10m,
                    sourcePacks.Count == 0
                        ? "No source of truth packs defined"
                        : $"{sourcePacks.Count} source of truth pack(s) — coverage {sourceScore:F1}/10"),
                new("changeValidation", ScoreToMaturity(changeScore), changeScore, 10m,
                    changePacks.Count == 0
                        ? "No change governance packs defined"
                        : $"{changePacks.Count} change governance pack(s) — coverage {changeScore:F1}/10"),
                new("operationalReadiness", ScoreToMaturity(operationsScore), operationsScore, 10m,
                    operationsPacks.Count == 0
                        ? "No operations packs defined"
                        : $"{operationsPacks.Count} operations pack(s) — coverage {operationsScore:F1}/10"),
                new("aiGovernance", ScoreToMaturity(aiScore), aiScore, 10m,
                    aiGovPacks.Count == 0
                        ? "No AI governance packs defined"
                        : $"{aiGovPacks.Count} AI governance pack(s) — coverage {aiScore:F1}/10"),
            };

            var overallScore = dimensions.Average(d => d.Score);
            var overallMaturity = ScoreToMaturity(overallScore);

            return new MaturityScorecardDto(
                GroupId: teamId,
                GroupName: team.DisplayName,
                OverallMaturity: overallMaturity,
                Dimensions: dimensions);
        }

        private static decimal ComputeDimensionScore(
            IReadOnlyList<GovernancePack> packs,
            IReadOnlyList<GovernanceRolloutRecord> teamRollouts,
            IReadOnlyList<GovernanceWaiver> teamWaivers)
        {
            if (packs.Count == 0) return 0m;

            var completedCount = packs.Count(p =>
                teamRollouts.Any(r => r.PackId == p.Id && r.Status == RolloutStatus.Completed));
            var pendingWaiverCount = packs.Count(p =>
                teamWaivers.Any(w => w.PackId == p.Id && w.Status == WaiverStatus.Pending));

            var rawScore = ((decimal)completedCount / packs.Count) * 10m;
            var penaltyScore = ((decimal)pendingWaiverCount / packs.Count) * 2m;

            return Math.Max(0m, Math.Round(rawScore - penaltyScore, 1));
        }

        private static MaturityLevel ScoreToMaturity(decimal score) =>
            score >= 9m ? MaturityLevel.Optimizing
            : score >= 7m ? MaturityLevel.Managed
            : score >= 5m ? MaturityLevel.Defined
            : score >= 2m ? MaturityLevel.Developing
            : MaturityLevel.Initial;
    }

    /// <summary>Resposta dos scorecards de maturidade por grupo.</summary>
    public sealed record Response(
        string Dimension,
        IReadOnlyList<MaturityScorecardDto> Scorecards,
        DateTimeOffset GeneratedAt);

    /// <summary>Scorecard de maturidade para um grupo com avaliação por dimensão.</summary>
    public sealed record MaturityScorecardDto(
        string GroupId,
        string GroupName,
        MaturityLevel OverallMaturity,
        IReadOnlyList<MaturityDimensionScoreDto> Dimensions);

    /// <summary>Pontuação de uma dimensão de maturidade com explicação.</summary>
    public sealed record MaturityDimensionScoreDto(
        string Dimension,
        MaturityLevel Level,
        decimal Score,
        decimal MaxScore,
        string Explanation);
}

