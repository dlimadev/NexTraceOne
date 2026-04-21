using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetTeamChangeVelocityReport;

/// <summary>
/// Feature: GetTeamChangeVelocityReport — relatório de velocidade de mudança por equipa.
///
/// Agrega releases num período e computa por equipa:
/// - total de releases
/// - releases por semana (velocity)
/// - taxa de sucesso (Succeeded / total)
/// - taxa de rollback (RolledBack / total)
/// - taxa de falha (Failed / total)
/// - velocidade relativa ao total do tenant (percentagem)
///
/// Permite aos Tech Leads e Architects comparar a cadência de mudança entre equipas
/// e identificar equipas com alta taxa de rollback ou falha.
///
/// Wave M.2 — Team Change Velocity Report (ChangeGovernance Change Intelligence).
/// </summary>
public static class GetTeamChangeVelocityReport
{
    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>Environment</c>: ambiente para filtrar releases (opcional — null = todos).</para>
    /// <para><c>LookbackDays</c>: janela temporal em dias (1–365, default 90).</para>
    /// <para><c>TopTeamsCount</c>: número máximo de equipas no relatório (1–100, default 20).</para>
    /// </summary>
    public sealed record Query(
        Guid TenantId,
        string? Environment = null,
        int LookbackDays = 90,
        int TopTeamsCount = 20) : IQuery<Report>;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Classificação de velocidade de mudança de uma equipa.</summary>
    public enum VelocityTier
    {
        /// <summary>≥ 4 releases/semana — equipa de alto volume de mudança.</summary>
        HighVolume,
        /// <summary>≥ 1 release/semana — equipa com ritmo saudável.</summary>
        Moderate,
        /// <summary>≥ 0.25 releases/semana — equipa de baixa frequência.</summary>
        LowFrequency,
        /// <summary>< 0.25 releases/semana — equipa inativa no período.</summary>
        Inactive
    }

    /// <summary>Métricas de velocidade por equipa.</summary>
    public sealed record TeamVelocityMetrics(
        string TeamName,
        int TotalReleases,
        int SucceededReleases,
        int FailedReleases,
        int RolledBackReleases,
        decimal ReleasesPerWeek,
        decimal SuccessRate,
        decimal FailureRate,
        decimal RollbackRate,
        decimal ShareOfTenantReleases,
        VelocityTier VelocityTier);

    /// <summary>Resultado do relatório de velocidade de mudança por equipa.</summary>
    public sealed record Report(
        int TotalReleases,
        int TeamsAnalyzed,
        int TeamsWithRollbacks,
        decimal TenantSuccessRate,
        decimal TenantRollbackRate,
        IReadOnlyList<TeamVelocityMetrics> TeamMetrics,
        Guid TenantId,
        int LookbackDays,
        DateTimeOffset From,
        DateTimeOffset To,
        DateTimeOffset GeneratedAt);

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(q => q.TopTeamsCount).InclusiveBetween(1, 100);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IDateTimeProvider clock)
        : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.Null(query);

            var to = clock.UtcNow;
            var from = to.AddDays(-query.LookbackDays);

            var releases = await releaseRepository.ListInRangeAsync(
                from, to, query.Environment, query.TenantId, cancellationToken);

            var totalReleases = releases.Count;
            var weeks = Math.Max(1m, (decimal)query.LookbackDays / 7m);

            if (totalReleases == 0)
            {
                return Result<Report>.Success(new Report(
                    TotalReleases: 0,
                    TeamsAnalyzed: 0,
                    TeamsWithRollbacks: 0,
                    TenantSuccessRate: 0m,
                    TenantRollbackRate: 0m,
                    TeamMetrics: [],
                    TenantId: query.TenantId,
                    LookbackDays: query.LookbackDays,
                    From: from,
                    To: to,
                    GeneratedAt: clock.UtcNow));
            }

            // Tenant-level rates
            var tenantSucceeded = releases.Count(r => r.Status == DeploymentStatus.Succeeded);
            var tenantRolledBack = releases.Count(r => r.Status == DeploymentStatus.RolledBack);
            var tenantSuccessRate = Math.Round((decimal)tenantSucceeded / totalReleases * 100m, 1);
            var tenantRollbackRate = Math.Round((decimal)tenantRolledBack / totalReleases * 100m, 1);

            // Group by team — releases without TeamName are grouped under "unassigned"
            var byTeam = releases
                .GroupBy(r => string.IsNullOrWhiteSpace(r.TeamName) ? "unassigned" : r.TeamName)
                .Select(g =>
                {
                    var teamTotal = g.Count();
                    var succeeded = g.Count(r => r.Status == DeploymentStatus.Succeeded);
                    var failed = g.Count(r => r.Status == DeploymentStatus.Failed);
                    var rolledBack = g.Count(r => r.Status == DeploymentStatus.RolledBack);

                    var releasesPerWeek = Math.Round((decimal)teamTotal / weeks, 2);
                    var successRate = Math.Round((decimal)succeeded / teamTotal * 100m, 1);
                    var failureRate = Math.Round((decimal)failed / teamTotal * 100m, 1);
                    var rollbackRate = Math.Round((decimal)rolledBack / teamTotal * 100m, 1);
                    var shareOfTenant = Math.Round((decimal)teamTotal / totalReleases * 100m, 1);

                    return new TeamVelocityMetrics(
                        TeamName: g.Key,
                        TotalReleases: teamTotal,
                        SucceededReleases: succeeded,
                        FailedReleases: failed,
                        RolledBackReleases: rolledBack,
                        ReleasesPerWeek: releasesPerWeek,
                        SuccessRate: successRate,
                        FailureRate: failureRate,
                        RollbackRate: rollbackRate,
                        ShareOfTenantReleases: shareOfTenant,
                        VelocityTier: ClassifyVelocity(releasesPerWeek));
                })
                .OrderByDescending(t => t.TotalReleases)
                .Take(query.TopTeamsCount)
                .ToList();

            var teamsWithRollbacks = byTeam.Count(t => t.RolledBackReleases > 0);

            return Result<Report>.Success(new Report(
                TotalReleases: totalReleases,
                TeamsAnalyzed: byTeam.Count,
                TeamsWithRollbacks: teamsWithRollbacks,
                TenantSuccessRate: tenantSuccessRate,
                TenantRollbackRate: tenantRollbackRate,
                TeamMetrics: byTeam,
                TenantId: query.TenantId,
                LookbackDays: query.LookbackDays,
                From: from,
                To: to,
                GeneratedAt: clock.UtcNow));
        }

        private static VelocityTier ClassifyVelocity(decimal releasesPerWeek)
            => releasesPerWeek >= 4m ? VelocityTier.HighVolume
             : releasesPerWeek >= 1m ? VelocityTier.Moderate
             : releasesPerWeek >= 0.25m ? VelocityTier.LowFrequency
             : VelocityTier.Inactive;
    }
}
