using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ProductAnalytics.Application.Abstractions;

namespace NexTraceOne.ProductAnalytics.Application.Features.GetPortalAdoptionFunnelReport;

/// <summary>
/// Análise do funil de adopção do NexTraceOne como Developer Portal.
/// Responde: onde quebra o funil de adopção por feature e equipa — quem está Inactive, quem está Lagging,
/// quais as oportunidades de enablement com maior impacto potencial?
/// Orientado para Platform Admin e Product.
/// </summary>
public static class GetPortalAdoptionFunnelReport
{
    // ── Enums ─────────────────────────────────────────────────────────────

    /// <summary>Tier de adopção por equipa.</summary>
    public enum TeamAdoptionTier
    {
        /// <summary>Adopção ≥80% de features com PowerUser.</summary>
        Leader,
        /// <summary>Adopção ≥50%.</summary>
        Active,
        /// <summary>Adopção ≥20%.</summary>
        Lagging,
        /// <summary>Adopção <20% ou sem actividade recente.</summary>
        Inactive
    }

    /// <summary>Direcção de tendência de adopção.</summary>
    public enum AdoptionTrendDirection { Growing, Stable, Declining }

    // ── Query ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Parâmetros de consulta do funil de adopção.
    /// </summary>
    /// <param name="LookbackDays">Dias de lookback para análise do funil. Padrão: 30.</param>
    /// <param name="InactiveUserDays">Dias sem login para considerar utilizador inactivo. Padrão: 30.</param>
    /// <param name="ActiveSessionsMin">Sessões mínimas para classificar utilizador como Active. Padrão: 3.</param>
    /// <param name="LeaderThreshold">Score mínimo (0–100) para AdoptionTier Leader. Padrão: 80.</param>
    /// <param name="ActiveThreshold">Score mínimo para AdoptionTier Active. Padrão: 50.</param>
    /// <param name="LaggingThreshold">Score mínimo para AdoptionTier Lagging. Padrão: 20.</param>
    /// <param name="TopEnablementOpportunities">Número máximo de oportunidades de enablement a retornar. Padrão: 5.</param>
    public sealed record Query(
        int LookbackDays = 30,
        int InactiveUserDays = 30,
        int ActiveSessionsMin = 3,
        decimal LeaderThreshold = 80m,
        decimal ActiveThreshold = 50m,
        decimal LaggingThreshold = 20m,
        int TopEnablementOpportunities = 5) : IQuery<Response>;

    // ── Response ──────────────────────────────────────────────────────────

    /// <summary>Resposta do relatório de funil de adopção do portal.</summary>
    /// <param name="ByTeam">Análise de adopção por equipa.</param>
    /// <param name="Summary">Sumário de adopção no tenant.</param>
    /// <param name="InactiveUsers">Utilizadores com licença mas sem login no período.</param>
    /// <param name="EnablementOpportunityList">Top N combinações (equipa × feature) com maior gap de adopção.</param>
    /// <param name="HistoricalAdoptionTrend">Tendência de adopção nos últimos 90 dias.</param>
    public sealed record Response(
        IReadOnlyList<TeamAdoptionResult> ByTeam,
        TenantAdoptionFunnelSummary Summary,
        IReadOnlyList<InactiveUserDto> InactiveUsers,
        IReadOnlyList<EnablementOpportunityDto> EnablementOpportunityList,
        AdoptionTrendDirection HistoricalAdoptionTrend);

    /// <summary>Resultado de adopção para uma equipa.</summary>
    public sealed record TeamAdoptionResult(
        string TeamId,
        string TeamName,
        decimal OverallAdoptionScore,
        TeamAdoptionTier AdoptionTier,
        DateTimeOffset? LastActiveAt,
        IReadOnlyList<FeatureFunnelDto> FeatureFunnel,
        IReadOnlyList<string> FeatureGaps);

    /// <summary>Funil de adopção por feature para uma equipa.</summary>
    public sealed record FeatureFunnelDto(
        string FeatureName,
        int AwareUsers,
        int ActiveUsers,
        int PowerUsers,
        decimal FunnelDropRate);

    /// <summary>Sumário de adopção para o tenant.</summary>
    public sealed record TenantAdoptionFunnelSummary(
        decimal ActiveUserRate,
        decimal TenantAdoptionScore,
        IReadOnlyList<string> MostAdoptedFeatures,
        IReadOnlyList<string> LeastAdoptedFeatures,
        int TotalLicensedUsers,
        int TotalActiveUsers,
        int InactiveUserCount,
        int LeaderTeams,
        int InactiveTeams);

    /// <summary>Utilizador inactivo com detalhes de licença.</summary>
    public sealed record InactiveUserDto(
        string UserId,
        string UserName,
        string TeamName,
        DateTimeOffset? LastLoginAt);

    /// <summary>Oportunidade de enablement (equipa × feature).</summary>
    public sealed record EnablementOpportunityDto(
        string TeamName,
        string FeatureName,
        decimal GapScore,
        string Rationale);

    // ── Validator ─────────────────────────────────────────────────────────

    /// <summary>Validador da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        /// <summary>Inicializa regras de validação.</summary>
        public Validator()
        {
            RuleFor(x => x.LookbackDays).GreaterThan(0);
            RuleFor(x => x.InactiveUserDays).GreaterThan(0);
            RuleFor(x => x.ActiveSessionsMin).GreaterThan(0);
            RuleFor(x => x.LeaderThreshold).InclusiveBetween(0m, 100m);
            RuleFor(x => x.ActiveThreshold).InclusiveBetween(0m, 100m);
            RuleFor(x => x.LaggingThreshold).InclusiveBetween(0m, 100m);
            RuleFor(x => x.TopEnablementOpportunities).GreaterThan(0);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    /// <summary>Handler que calcula o funil de adopção do portal por equipa e feature.</summary>
    public sealed class Handler(
        IPortalAdoptionReader reader,
        IDateTimeProvider clock,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        /// <inheritdoc/>
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;
            var from = now.AddDays(-request.LookbackDays);
            var inactiveSince = now.AddDays(-request.InactiveUserDays);

            var tenantId = currentTenant.Id.ToString();
            var teamTask = reader.ListTeamAdoptionAsync(tenantId, from, now, cancellationToken);
            var inactiveTask = reader.ListInactiveUsersAsync(tenantId, inactiveSince, cancellationToken);
            var trendTask = reader.GetAdoptionTrendAsync(tenantId, now.AddDays(-90), now, cancellationToken);
            await Task.WhenAll(teamTask, inactiveTask, trendTask);
            var teamEntries = teamTask.Result;
            var inactiveUsers = inactiveTask.Result;
            var trendSnapshots = trendTask.Result;

            var teamResults = teamEntries.Select(t => BuildTeamResult(t, request)).ToList();

            // ── Summary ──────────────────────────────────────────────────
            var totalLicensed = teamEntries.Sum(t => t.TotalMembers);
            var totalActive = teamResults.Sum(t => t.FeatureFunnel.Sum(f => f.ActiveUsers));
            var activeUserRate = totalLicensed > 0
                ? Math.Round((decimal)teamResults.Count(t => t.LastActiveAt >= from) /
                             Math.Max(1, teamEntries.Sum(t => t.TotalMembers)) * 100m, 1)
                : 0m;
            var tenantAdoptionScore = teamResults.Count > 0
                ? Math.Round((decimal)teamResults.Count(t =>
                    t.AdoptionTier is TeamAdoptionTier.Leader or TeamAdoptionTier.Active) /
                    teamResults.Count * 100m, 1)
                : 100m;

            var allFeatureStats = teamEntries
                .SelectMany(t => t.FeatureStats)
                .GroupBy(f => f.FeatureName)
                .Select(g => (
                    Feature: g.Key,
                    PowerUserRate: g.Sum(x => x.AwareUsers) > 0
                        ? (decimal)g.Sum(x => x.PowerUsers) / g.Sum(x => x.AwareUsers) * 100m
                        : 0m))
                .OrderByDescending(x => x.PowerUserRate)
                .ToList();

            var mostAdopted = allFeatureStats.Take(3).Select(x => x.Feature).ToList();
            var leastAdopted = allFeatureStats.TakeLast(3).Select(x => x.Feature).ToList();

            var summary = new TenantAdoptionFunnelSummary(
                ActiveUserRate: activeUserRate,
                TenantAdoptionScore: tenantAdoptionScore,
                MostAdoptedFeatures: mostAdopted,
                LeastAdoptedFeatures: leastAdopted,
                TotalLicensedUsers: totalLicensed,
                TotalActiveUsers: totalActive,
                InactiveUserCount: inactiveUsers.Count,
                LeaderTeams: teamResults.Count(t => t.AdoptionTier == TeamAdoptionTier.Leader),
                InactiveTeams: teamResults.Count(t => t.AdoptionTier == TeamAdoptionTier.Inactive));

            // ── Enablement Opportunities ─────────────────────────────────
            var opportunities = BuildEnablementOpportunities(teamResults, request.TopEnablementOpportunities);

            // ── Historical Trend ─────────────────────────────────────────
            var trend = ComputeTrend(trendSnapshots);

            var inactiveDtos = inactiveUsers
                .Select(u => new InactiveUserDto(u.UserId, u.UserName, u.TeamName, u.LastLoginAt))
                .ToList();

            return Result<Response>.Success(new Response(
                ByTeam: teamResults,
                Summary: summary,
                InactiveUsers: inactiveDtos,
                EnablementOpportunityList: opportunities,
                HistoricalAdoptionTrend: trend));
        }

        private TeamAdoptionResult BuildTeamResult(
            IPortalAdoptionReader.TeamAdoptionEntry entry,
            Query request)
        {
            var funnel = entry.FeatureStats.Select(f =>
            {
                var dropRate = f.AwareUsers > 0
                    ? Math.Round((decimal)(f.AwareUsers - f.ActiveUsers) / f.AwareUsers * 100m, 1)
                    : 0m;
                return new FeatureFunnelDto(f.FeatureName, f.AwareUsers, f.ActiveUsers, f.PowerUsers, dropRate);
            }).ToList();

            var totalFeatures = entry.FeatureStats.Count;
            var featuresWithPowerUser = entry.FeatureStats.Count(f => f.PowerUsers > 0);
            var score = totalFeatures > 0
                ? Math.Round((decimal)featuresWithPowerUser / totalFeatures * 100m, 1)
                : 0m;

            var tier = score >= request.LeaderThreshold ? TeamAdoptionTier.Leader
                : score >= request.ActiveThreshold ? TeamAdoptionTier.Active
                : score >= request.LaggingThreshold ? TeamAdoptionTier.Lagging
                : TeamAdoptionTier.Inactive;

            var gaps = entry.FeatureStats
                .Where(f => f.PowerUsers == 0)
                .Select(f => f.FeatureName)
                .ToList();

            return new TeamAdoptionResult(
                TeamId: entry.TeamId,
                TeamName: entry.TeamName,
                OverallAdoptionScore: score,
                AdoptionTier: tier,
                LastActiveAt: entry.LastActiveAt,
                FeatureFunnel: funnel,
                FeatureGaps: gaps);
        }

        private static List<EnablementOpportunityDto> BuildEnablementOpportunities(
            List<TeamAdoptionResult> teams,
            int topN)
        {
            return teams
                .Where(t => t.AdoptionTier is TeamAdoptionTier.Lagging or TeamAdoptionTier.Inactive)
                .SelectMany(t => t.FeatureGaps.Select(f => (
                    Team: t.TeamName,
                    Feature: f,
                    GapScore: 100m - t.OverallAdoptionScore)))
                .OrderByDescending(x => x.GapScore)
                .Take(topN)
                .Select(x => new EnablementOpportunityDto(
                    TeamName: x.Team,
                    FeatureName: x.Feature,
                    GapScore: x.GapScore,
                    Rationale: $"Team has not used {x.Feature} — targeted enablement session recommended"))
                .ToList();
        }

        private static AdoptionTrendDirection ComputeTrend(
            IReadOnlyList<IPortalAdoptionReader.DailyAdoptionSnapshot> snapshots)
        {
            if (snapshots.Count < 2) return AdoptionTrendDirection.Stable;
            var first = snapshots.OrderByDescending(s => s.DaysAgo).First();
            var last = snapshots.OrderBy(s => s.DaysAgo).First();
            var firstRate = first.TotalLicensedUsers > 0
                ? (decimal)first.ActiveUsers / first.TotalLicensedUsers
                : 0m;
            var lastRate = last.TotalLicensedUsers > 0
                ? (decimal)last.ActiveUsers / last.TotalLicensedUsers
                : 0m;
            if (lastRate > firstRate * 1.05m) return AdoptionTrendDirection.Growing;
            if (lastRate < firstRate * 0.95m) return AdoptionTrendDirection.Declining;
            return AdoptionTrendDirection.Stable;
        }
    }
}
