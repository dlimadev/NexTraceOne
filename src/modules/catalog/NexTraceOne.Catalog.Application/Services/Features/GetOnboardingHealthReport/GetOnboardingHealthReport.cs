using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Services.Abstractions;

namespace NexTraceOne.Catalog.Application.Services.Features.GetOnboardingHealthReport;

/// <summary>
/// Feature: GetOnboardingHealthReport — scorecard de completude de onboarding por serviço e equipa.
///
/// Avalia cada serviço do tenant em cinco dimensões ponderadas:
/// - Ownership (20 pontos)
/// - Contrato aprovado (25 pontos)
/// - Runbook aprovado (20 pontos)
/// - Observação de SLO recente, últimos 30 dias (20 pontos)
/// - Profiling recente, últimos 90 dias (15 pontos)
///
/// Atribui um <see cref="OnboardingTier"/> por serviço com base em thresholds configuráveis,
/// calcula médias por equipa e produz um score global de onboarding para o tenant.
///
/// Wave AC.1 — GetOnboardingHealthReport (Catalog).
/// </summary>
public static class GetOnboardingHealthReport
{
    // ── Tier de onboarding ────────────────────────────────────────────────

    /// <summary>Nível de completude de onboarding de um serviço.</summary>
    public enum OnboardingTier
    {
        /// <summary>Score ≥ threshold completo (padrão: 90).</summary>
        Complete,
        /// <summary>Score ≥ threshold avançado (padrão: 70) e &lt; completo.</summary>
        Advanced,
        /// <summary>Score ≥ threshold básico (padrão: 40) e &lt; avançado.</summary>
        Basic,
        /// <summary>Score &lt; threshold básico.</summary>
        Minimal
    }

    // ── Constantes de pontuação ───────────────────────────────────────────

    private const int OwnershipWeight = 20;
    private const int ContractsWeight = 25;
    private const int RunbookWeight = 20;
    private const int SloWeight = 20;
    private const int ProfilingWeight = 15;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Score de onboarding calculado para um serviço.</summary>
    public sealed record ServiceOnboardingScore(
        string ServiceName,
        string? TeamName,
        string ServiceTier,
        int OnboardingScore,
        OnboardingTier Tier,
        bool HasOwnership,
        bool HasApprovedContract,
        bool HasApprovedRunbook,
        bool HasRecentSlo,
        bool HasRecentProfiling);

    /// <summary>Distribuição de serviços por tier de onboarding.</summary>
    public sealed record OnboardingTierDistribution(
        int CompleteCount,
        int AdvancedCount,
        int BasicCount,
        int MinimalCount);

    /// <summary>Média de onboarding de uma equipa.</summary>
    public sealed record TeamOnboardingAvg(string TeamName, double AvgScore, int ServiceCount);

    /// <summary>Relatório completo de onboarding health do tenant.</summary>
    public sealed record Report(
        string TenantId,
        IReadOnlyList<ServiceOnboardingScore> Services,
        IReadOnlyList<ServiceOnboardingScore> TopLowestScores,
        OnboardingTierDistribution TierDistribution,
        IReadOnlyList<TeamOnboardingAvg> TeamAverages,
        double TenantOnboardingScore,
        int TotalServicesAnalyzed);

    // ── Query ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>CompleteThreshold</c>: pontuação mínima para tier Complete (1–100, padrão 90).</para>
    /// <para><c>AdvancedThreshold</c>: pontuação mínima para tier Advanced (1–100, padrão 70).</para>
    /// <para><c>BasicThreshold</c>: pontuação mínima para tier Basic (1–100, padrão 40).</para>
    /// <para><c>TopLowestCount</c>: número de serviços com menor score a incluir (1–50, padrão 10).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int CompleteThreshold = 90,
        int AdvancedThreshold = 70,
        int BasicThreshold = 40,
        int TopLowestCount = 10) : IQuery<Report>;

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.CompleteThreshold).InclusiveBetween(1, 100);
            RuleFor(q => q.AdvancedThreshold).InclusiveBetween(1, 100);
            RuleFor(q => q.BasicThreshold).InclusiveBetween(1, 100);
            RuleFor(q => q.TopLowestCount).InclusiveBetween(1, 50);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IOnboardingHealthReader _reader;

        public Handler(IOnboardingHealthReader reader)
        {
            _reader = Guard.Against.Null(reader);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var entries = await _reader.ListByTenantAsync(query.TenantId, cancellationToken);

            if (entries.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    TenantId: query.TenantId,
                    Services: [],
                    TopLowestScores: [],
                    TierDistribution: new OnboardingTierDistribution(0, 0, 0, 0),
                    TeamAverages: [],
                    TenantOnboardingScore: 0.0,
                    TotalServicesAnalyzed: 0));
            }

            var scored = entries.Select(e => ComputeScore(e, query)).ToList();

            var topLowest = scored
                .OrderBy(s => s.OnboardingScore)
                .Take(query.TopLowestCount)
                .ToList();

            var tierDistribution = new OnboardingTierDistribution(
                CompleteCount: scored.Count(s => s.Tier == OnboardingTier.Complete),
                AdvancedCount: scored.Count(s => s.Tier == OnboardingTier.Advanced),
                BasicCount: scored.Count(s => s.Tier == OnboardingTier.Basic),
                MinimalCount: scored.Count(s => s.Tier == OnboardingTier.Minimal));

            var teamAverages = scored
                .Where(s => !string.IsNullOrWhiteSpace(s.TeamName))
                .GroupBy(s => s.TeamName!)
                .Select(g => new TeamOnboardingAvg(
                    TeamName: g.Key,
                    AvgScore: g.Average(s => s.OnboardingScore),
                    ServiceCount: g.Count()))
                .OrderByDescending(t => t.AvgScore)
                .ToList();

            var tenantScore = scored.Average(s => s.OnboardingScore);

            return Result<Report>.Success(new Report(
                TenantId: query.TenantId,
                Services: scored,
                TopLowestScores: topLowest,
                TierDistribution: tierDistribution,
                TeamAverages: teamAverages,
                TenantOnboardingScore: tenantScore,
                TotalServicesAnalyzed: scored.Count));
        }

        private static ServiceOnboardingScore ComputeScore(ServiceOnboardingEntry entry, Query query)
        {
            var score =
                (entry.HasOwnership ? OwnershipWeight : 0) +
                (entry.HasApprovedContract ? ContractsWeight : 0) +
                (entry.HasApprovedRunbook ? RunbookWeight : 0) +
                (entry.HasRecentSloObservation ? SloWeight : 0) +
                (entry.HasRecentProfiling ? ProfilingWeight : 0);

            var tier = score >= query.CompleteThreshold ? OnboardingTier.Complete
                : score >= query.AdvancedThreshold ? OnboardingTier.Advanced
                : score >= query.BasicThreshold ? OnboardingTier.Basic
                : OnboardingTier.Minimal;

            return new ServiceOnboardingScore(
                ServiceName: entry.ServiceName,
                TeamName: entry.TeamName,
                ServiceTier: entry.ServiceTier,
                OnboardingScore: score,
                Tier: tier,
                HasOwnership: entry.HasOwnership,
                HasApprovedContract: entry.HasApprovedContract,
                HasApprovedRunbook: entry.HasApprovedRunbook,
                HasRecentSlo: entry.HasRecentSloObservation,
                HasRecentProfiling: entry.HasRecentProfiling);
        }
    }
}
