using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetDeploymentFrequencyHealthReport;

/// <summary>
/// Feature: GetDeploymentFrequencyHealthReport — saúde da frequência de deployments por serviço.
///
/// Calcula por serviço:
/// - frequência de deploy por mês
/// - idade do último deploy
/// - gap médio entre deployments
/// - tier: Optimal / Underdeploying / Overdeploying / Stale
/// - flag de alta variabilidade de cadência
///
/// Produz sumário do tenant: serviços stale, overdeploying, score de saúde,
/// comparação de frequência por equipa e distribuição por tier de serviço.
///
/// Wave AW.3 — Deployment Frequency Health Report (ChangeGovernance ChangeIntelligence).
/// </summary>
public static class GetDeploymentFrequencyHealthReport
{
    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal em dias (1–365, default 90).</para>
    /// <para><c>StaleDeployDays</c>: dias sem deploy para classificar serviço como stale (default 60).</para>
    /// <para><c>HighVariabilityThreshold</c>: ratio stddev/mean de gap que caracteriza alta variabilidade (default 0.5).</para>
    /// <para><c>MaxServices</c>: número máximo de serviços no relatório (1–200, default 20).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 90,
        int StaleDeployDays = 60,
        decimal HighVariabilityThreshold = 0.5m,
        int MaxServices = 20) : IQuery<Report>;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Tier de frequência de deploy.</summary>
    public enum DeployFrequencyTier
    {
        /// <summary>Frequência entre 2 e 20 deploys por mês — cadência saudável.</summary>
        Optimal,
        /// <summary>Frequência abaixo de 2 deploys por mês — ritmo baixo.</summary>
        Underdeploying,
        /// <summary>Frequência acima de 20 deploys por mês — ritmo excessivo.</summary>
        Overdeploying,
        /// <summary>Sem deploys nos últimos StaleDeployDays dias.</summary>
        Stale
    }

    /// <summary>Métricas de frequência de deploy de um serviço.</summary>
    public sealed record ServiceDeployFrequency(
        string ServiceName,
        string TeamName,
        string ServiceTier,
        double DeployFrequencyPerMonth,
        double LastDeployAge,
        double DeployGap,
        DeployFrequencyTier Tier,
        bool HighVariabilityFlag);

    /// <summary>Sumário de saúde de frequência de deploy do tenant.</summary>
    public sealed record TenantDeployFrequencySummary(
        IReadOnlyDictionary<string, int> DeploysByTier,
        IReadOnlyList<string> StaleServices,
        IReadOnlyList<string> OverdeployingServices,
        decimal TenantDeployFrequencyHealthScore);

    /// <summary>Resultado do relatório de saúde da frequência de deployments.</summary>
    public sealed record Report(
        IReadOnlyList<ServiceDeployFrequency> Services,
        TenantDeployFrequencySummary Summary,
        IReadOnlyDictionary<string, double> TeamDeployFrequencyComparison,
        IReadOnlyList<ServiceDeployFrequency> DeployFrequencyVsIncidentRate,
        string TenantId,
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
            RuleFor(q => q.MaxServices).InclusiveBetween(1, 200);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IDeploymentFrequencyReader reader,
        IDateTimeProvider clock)
        : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.Null(query);
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var to = clock.UtcNow;
            var from = to.AddDays(-query.LookbackDays);

            var deployments = await reader.ListDeploymentsByTenantAsync(
                query.TenantId, from, to, cancellationToken);

            if (deployments.Count == 0)
            {
                var emptyTierDict = new Dictionary<string, int>();
                return Result<Report>.Success(new Report(
                    Services: [],
                    Summary: new TenantDeployFrequencySummary(
                        DeploysByTier: emptyTierDict,
                        StaleServices: [],
                        OverdeployingServices: [],
                        TenantDeployFrequencyHealthScore: 0m),
                    TeamDeployFrequencyComparison: new Dictionary<string, double>(),
                    DeployFrequencyVsIncidentRate: [],
                    TenantId: query.TenantId,
                    LookbackDays: query.LookbackDays,
                    From: from,
                    To: to,
                    GeneratedAt: clock.UtcNow));
            }

            var monthsInPeriod = query.LookbackDays / 30.0;

            // ── Per-service metrics ───────────────────────────────────────────

            var services = deployments
                .GroupBy(d => d.ServiceName, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var deploys = g.OrderBy(d => d.DeployedAt).ToList();
                    var deployCount = deploys.Count;
                    var lastDeploy = deploys.Last();
                    var firstEntry = deploys.First();

                    var freqPerMonth = Math.Round(deployCount / monthsInPeriod, 2);
                    var lastDeployAge = Math.Round((to - lastDeploy.DeployedAt).TotalDays, 1);

                    double avgGap = 0;
                    if (deploys.Count > 1)
                    {
                        var gaps = deploys
                            .Zip(deploys.Skip(1), (a, b) => (b.DeployedAt - a.DeployedAt).TotalDays)
                            .ToList();
                        avgGap = Math.Round(gaps.Average(), 2);

                        var stdDev = gaps.Count > 1
                            ? Math.Sqrt(gaps.Average(gap => Math.Pow(gap - avgGap, 2)))
                            : 0;
                        var highVariability = avgGap > 0 && (decimal)(stdDev / avgGap) > query.HighVariabilityThreshold;

                        var tier = ClassifyTier(lastDeployAge, freqPerMonth, query.StaleDeployDays);

                        return new ServiceDeployFrequency(
                            ServiceName: g.Key,
                            TeamName: firstEntry.TeamName,
                            ServiceTier: firstEntry.ServiceTier,
                            DeployFrequencyPerMonth: freqPerMonth,
                            LastDeployAge: lastDeployAge,
                            DeployGap: avgGap,
                            Tier: tier,
                            HighVariabilityFlag: highVariability);
                    }
                    else
                    {
                        var tier = ClassifyTier(lastDeployAge, freqPerMonth, query.StaleDeployDays);
                        return new ServiceDeployFrequency(
                            ServiceName: g.Key,
                            TeamName: firstEntry.TeamName,
                            ServiceTier: firstEntry.ServiceTier,
                            DeployFrequencyPerMonth: freqPerMonth,
                            LastDeployAge: lastDeployAge,
                            DeployGap: 0,
                            Tier: tier,
                            HighVariabilityFlag: false);
                    }
                })
                .OrderByDescending(s => s.DeployFrequencyPerMonth)
                .Take(query.MaxServices)
                .ToList();

            // ── Summary ───────────────────────────────────────────────────────

            var deploysByTier = services
                .GroupBy(s => s.ServiceTier, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count());

            var staleServices = services
                .Where(s => s.Tier == DeployFrequencyTier.Stale)
                .Select(s => s.ServiceName)
                .ToList();

            var overdeployingServices = services
                .Where(s => s.Tier == DeployFrequencyTier.Overdeploying)
                .Select(s => s.ServiceName)
                .ToList();

            var optimalCount = services.Count(s => s.Tier == DeployFrequencyTier.Optimal);
            var healthScore = services.Count > 0
                ? Math.Round((decimal)optimalCount / services.Count * 100m, 1)
                : 0m;

            var summary = new TenantDeployFrequencySummary(
                DeploysByTier: deploysByTier,
                StaleServices: staleServices,
                OverdeployingServices: overdeployingServices,
                TenantDeployFrequencyHealthScore: healthScore);

            // ── Team comparison ───────────────────────────────────────────────

            var teamComparison = services
                .GroupBy(s => s.TeamName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => Math.Round(g.Average(s => s.DeployFrequencyPerMonth), 2));

            // ── Deploy frequency vs incident rate (sorted by frequency) ───────

            var sortedByFrequency = services
                .OrderByDescending(s => s.DeployFrequencyPerMonth)
                .ToList();

            return Result<Report>.Success(new Report(
                Services: services,
                Summary: summary,
                TeamDeployFrequencyComparison: teamComparison,
                DeployFrequencyVsIncidentRate: sortedByFrequency,
                TenantId: query.TenantId,
                LookbackDays: query.LookbackDays,
                From: from,
                To: to,
                GeneratedAt: clock.UtcNow));
        }

        private static DeployFrequencyTier ClassifyTier(double lastDeployAge, double freqPerMonth, int staleDeployDays)
        {
            if (lastDeployAge > staleDeployDays) return DeployFrequencyTier.Stale;
            if (freqPerMonth > 20) return DeployFrequencyTier.Overdeploying;
            if (freqPerMonth >= 2) return DeployFrequencyTier.Optimal;
            return DeployFrequencyTier.Underdeploying;
        }
    }
}
